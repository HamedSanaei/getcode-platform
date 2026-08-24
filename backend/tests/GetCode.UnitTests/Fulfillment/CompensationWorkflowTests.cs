using GetCode.Application.Fulfillment;
using GetCode.Application.Orders;
using GetCode.Application.Providers;
using GetCode.Application.Wallets;
using GetCode.Application.Catalog;
using GetCode.Domain.Orders;
using GetCode.Domain.Wallets;

namespace GetCode.UnitTests.Fulfillment;

/// <summary>
/// M07-004: compensation workflow — races arbitrated by the state machine,
/// double refunds impossible (guard + ledger idempotency), ambiguous provider
/// cancellations go to reconciliation instead of auto-refund.
/// </summary>
public sealed class CompensationWorkflowTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order ReservedOrder(string opId = "op-9")
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "idem-cmp", 150m, "RUB",
            "RU", "telegram", "activation", 1, T0);
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        order.StartFulfillment();
        order.MarkProviderReserved(opId);
        return order;
    }

    private class ScriptedProvider : IVirtualNumberProvider
    {
        public ProviderErrorCode? CancelError { get; set; }
        public int CancelCalls { get; private set; }
        public string ProviderKey => "scripted";

        public virtual Task<ProviderResult> CancelAsync(string id, CancellationToken ct)
        {
            CancelCalls++;
            return Task.FromResult(CancelError is { } err
                ? ProviderResult.Failure(err, err == ProviderErrorCode.AmbiguousOutcome ? "ambiguous" : "failed")
                : ProviderResult.Success());
        }

        public Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(string id, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(ProviderSearchQuery q, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderResult<ProviderReservation>> ReserveAsync(ProviderReservationRequest r, CancellationToken ct) => throw new NotSupportedException();
    }

    /// <summary>Real WalletService over an in-memory ledger: replays dedupe, credits counted.</summary>
    private static WalletService NewLedgerWallet(out List<LedgerEntry> refundEntries)
    {
        var ledgerEntries = new List<LedgerEntry>();
        var wallet = Wallet.Open(Guid.NewGuid(), "RUB", T0);
        wallet.Credit(Money.FromMajor(1000m, "RUB"), LedgerEntryType.Deposit, T0); // funded balance
        refundEntries = ledgerEntries;

        return new WalletService(
            new SingleWalletRepo(wallet),
            new MemoryLedger(ledgerEntries),
            new OkUnitOfWork(),
            new NullOutbox());
    }

    private sealed class SingleWalletRepo(Wallet wallet) : IWalletRepository
    {
        public Task<Wallet?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Wallet?>(wallet);
        public Task<Wallet?> FindForUserAsync(Guid owner, string currency, CancellationToken ct) =>
            Task.FromResult<Wallet?>(wallet.Currency == currency ? wallet : null);
        public void Add(Wallet w) { }
    }

    private sealed class MemoryLedger(List<LedgerEntry> entries) : ILedgerRepository
    {
        public Task<LedgerEntry?> FindByIdempotencyKeyAsync(string key, CancellationToken ct) =>
            Task.FromResult(entries.FirstOrDefault(e => e.IdempotencyKey == key));
        public void Append(LedgerEntry entry) => entries.Add(entry);
        public Task<IReadOnlyList<LedgerEntry>> ListForWalletAsync(Guid id, int limit, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LedgerEntry>>(entries);
    }

    private sealed class OkUnitOfWork : IWalletUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(1);
        public Task ResetAsync(CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class NullOutbox : IOutboxCollector
    {
        public void Collect(string type, string payload, string? correlationId = null) { }
    }

    private sealed class MemoryOrderRepo(Order order) : IOrderRepository
    {
        public int Saves { get; private set; }
        public Task<Order?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Order?>(order);
        public Task<Order?> FindByIdempotencyKeyAsync(Guid c, string k, CancellationToken ct) => Task.FromResult<Order?>(order);
        public Task AddAsync(Order o, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Order o, CancellationToken ct) { Saves++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Cancel_then_refund_happy_path_is_explicit_and_persisted()
    {
        var provider = new ScriptedProvider();
        var wallet = NewLedgerWallet(out var refundEntries);
        var order = ReservedOrder();
        var repo = new MemoryOrderRepo(order);
        var workflow = new CompensationWorkflow(provider, wallet, repo);

        var outcome = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(CompensationWorkflow.Outcome.Refunded, outcome);
        Assert.Equal(1, provider.CancelCalls);      // provider released first
        Assert.Single(refundEntries);                // then credit once
        Assert.Equal(OrderPaymentState.Refunded, order.PaymentState);
        Assert.Equal(OrderFulfillmentState.Failed, order.FulfillmentState);
    }

    [Fact]
    public async Task Double_refund_is_impossible_across_replays()
    {
        var provider = new ScriptedProvider();
        var wallet = NewLedgerWallet(out var refundEntries);
        var order = ReservedOrder();
        var workflow = new CompensationWorkflow(provider, wallet, new MemoryOrderRepo(order));

        var first = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);
        // Replay after a crash: the refunded-order guard short-circuits.
        var second = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(CompensationWorkflow.Outcome.Refunded, first);
        Assert.Equal(CompensationWorkflow.Outcome.AlreadyRefunded, second);
        Assert.Single(refundEntries); // exactly one real credit ever
    }

    [Fact]
    public async Task Ambiguous_provider_cancellation_goes_to_reconciliation_not_refund()
    {
        var provider = new ScriptedProvider { CancelError = ProviderErrorCode.AmbiguousOutcome };
        var wallet = NewLedgerWallet(out var refundEntries);
        var order = ReservedOrder();
        var workflow = new CompensationWorkflow(provider, wallet, new MemoryOrderRepo(order));

        var outcome = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(CompensationWorkflow.Outcome.ReconciliationRequired, outcome);
        Assert.Empty(refundEntries);                    // NO money moved on ambiguity
        Assert.Equal(OrderPaymentState.Paid, order.PaymentState);   // untouched
        Assert.Equal(OrderFulfillmentState.Reserved, order.FulfillmentState);
    }

    [Fact]
    public async Task Race_with_message_arrival_is_arbitrated_by_the_state_machine()
    {
        var wallet = NewLedgerWallet(out var refundEntries);
        var order = ReservedOrder();
        // The polling side completes fulfillment DURING compensation: the
        // provider cancel returns while the poller flips the state.
        var provider = new RacingProvider(order);
        var workflow = new CompensationWorkflow(provider, wallet, new MemoryOrderRepo(order));

        var outcome = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(CompensationWorkflow.Outcome.RaceLostMessageArrived, outcome);
        Assert.Equal(OrderPaymentState.Paid, order.PaymentState); // refund NOT applied to a delivered order
        Assert.Empty(refundEntries); // no money moved on a lost race
    }

    /// <summary>Simulates the poller completing the order mid-compensation.</summary>
    private sealed class RacingProvider(Order order) : ScriptedProvider
    {
        public override async Task<ProviderResult> CancelAsync(string id, CancellationToken ct)
        {
            var result = await base.CancelAsync(id, ct);
            typeof(Order).GetProperty(nameof(Order.FulfillmentState))!
                .SetValue(order, OrderFulfillmentState.Completed); // poller wins concurrently
            return result;
        }
    }

    [Fact]
    public async Task Completed_order_is_rejected_without_provider_contact()
    {
        var provider = new ScriptedProvider();
        var wallet = NewLedgerWallet(out var refundEntries);
        var order = ReservedOrder();
        order.Complete(); // SMS arrived and was delivered
        var workflow = new CompensationWorkflow(provider, wallet, new MemoryOrderRepo(order));

        var outcome = await workflow.CancelAndRefundAsync(order, TestContext.Current.CancellationToken);

        Assert.Equal(CompensationWorkflow.Outcome.RejectedCompleted, outcome);
        Assert.Equal(0, provider.CancelCalls);  // nothing to cancel
        Assert.Empty(refundEntries);            // customer keeps the goods AND we keep the money
    }
}
