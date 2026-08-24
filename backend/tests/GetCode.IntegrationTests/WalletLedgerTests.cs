using GetCode.Application.Wallets;
using GetCode.Domain.Wallets;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M05-003 verification: ledger invariants (append-only, sum==balance, idempotency
/// identity) and concurrent-debit safety under optimistic concurrency.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class WalletLedgerTests(DatabaseFixture database)
{
    [Fact]
    public async Task Deposit_purchase_refund_chain_appends_compensating_entries()
    {
        await using var scope = new WalletScope(database);
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.CreateVersion7();
        var walletId = await scope.OpenWalletAsync(userId);

        var orderReferenceId = Guid.CreateVersion7();

        // Deposit 50, purchase 12.40 against an order, then refund that order.
        Assert.True((await scope.Service.DepositAsync(Cmd(userId, "dep-1", LedgerEntryType.Deposit, 50m), ct)).Success);
        var purchase = await scope.Service.PurchaseAsync(Cmd(userId, "buy-1", LedgerEntryType.Purchase, 12.40m, "order", orderReferenceId), ct);
        Assert.True(purchase.Success);
        Assert.Equal(3760, purchase.BalanceMinorAfter);

        var refund = await scope.Service.RefundAsync(Cmd(userId, "ref-1", LedgerEntryType.Refund, 12.40m, "order", orderReferenceId), ct);
        Assert.True(refund.Success);
        Assert.Equal(5000, refund.BalanceMinorAfter);

        // History grows; nothing was rewritten: three entries with distinct types.
        var entries = await scope.EntriesAsync(walletId);
        Assert.Equal(3, entries.Count);
        Assert.Equal(
            new[] { LedgerEntryType.Deposit, LedgerEntryType.Purchase, LedgerEntryType.Refund },
            entries.Select(e => e.EntryType).ToArray());

        // Ledger invariant: signed sum equals stored balance projection.
        Assert.Equal(5000, entries.Sum(e => e.AmountMinor));
    }

    [Fact]
    public async Task Duplicate_idempotency_key_replays_original_outcome_without_double_effect()
    {
        await using var scope = new WalletScope(database);
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.CreateVersion7();
        var walletId = await scope.OpenWalletAsync(userId);

        var first = await scope.Service.DepositAsync(Cmd(userId, "idem-1", LedgerEntryType.Deposit, 25m), ct);
        var replay = await scope.Service.DepositAsync(Cmd(userId, "idem-1", LedgerEntryType.Deposit, 25m), ct);

        Assert.True(first.Success);
        Assert.False(first.ReplayedExistingEntry);
        Assert.True(replay.ReplayedExistingEntry);
        Assert.Equal(first.BalanceMinorAfter, replay.BalanceMinorAfter);
        Assert.Single(await scope.EntriesAsync(walletId));
    }

    [Fact]
    public async Task Insufficient_balance_fails_without_negative_state()
    {
        await using var scope = new WalletScope(database);
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.CreateVersion7();
        await scope.OpenWalletAsync(userId);

        await scope.Service.DepositAsync(Cmd(userId, "seed-small", LedgerEntryType.Deposit, 3m), ct);
        var outcome = await scope.Service.PurchaseAsync(Cmd(userId, "too-big", LedgerEntryType.Purchase, 5m), ct);

        Assert.False(outcome.Success);
        Assert.Equal(300, outcome.BalanceMinorAfter); // untouched
        Assert.Single(await scope.EntriesAsync(await scope.WalletIdAsync(userId)));
    }

    [Fact]
    public async Task Concurrent_debits_cannot_overspend_or_lose_updates()
    {
        await using var scope = new WalletScope(database);
        var ct = TestContext.Current.CancellationToken;
        var userId = Guid.CreateVersion7();

        // Balance funds exactly 5 purchases of 1.00; fire 8 concurrently.
        await scope.OpenWalletAsync(userId);
        var walletId = await scope.WalletIdAsync(userId);
        await scope.Service.DepositAsync(Cmd(userId, "concurrent-seed", LedgerEntryType.Deposit, 5m), ct);
        Assert.Equal(500, await scope.BalanceAsync(walletId));

        const int concurrentCalls = 8;
        var tasks = Enumerable.Range(0, concurrentCalls)
            .Select(i => Task.Run(async () =>
            {
                await using var perCall = new WalletScope(database, ownsCleanup: false); // isolated context per call
                return await perCall.Service.PurchaseAsync(Cmd(userId, $"spend-{i:D2}", LedgerEntryType.Purchase, 1m), ct);
            }))
            .ToArray();

        var outcomes = await Task.WhenAll(tasks);

        Assert.Equal(5, outcomes.Count(o => o.Success));
        Assert.Equal(concurrentCalls - 5, outcomes.Count(o => !o.Success));
        Assert.All(outcomes.Where(o => !o.Success), o => Assert.True(o.BalanceMinorAfter >= 0));

        // Final state: zero balance, exactly 6 entries (1 deposit + 5 purchases), sum invariant holds.
        var entries = await scope.EntriesAsync(walletId);
        Assert.Equal(6, entries.Count);
        Assert.Equal(0, entries.Sum(e => e.AmountMinor));
        Assert.Equal(0, await scope.BalanceAsync(walletId));
    }

    private static WalletMutationCommand Cmd(Guid userId, string idempotencyKey, LedgerEntryType entryType, decimal amount, string? referenceType = null, Guid? referenceId = null) =>
        new(userId, entryType, amount, idempotencyKey, referenceType, referenceId);

    /// <summary>Owns factory + scope lifetime; only the owning scope resets wallet tables on dispose.</summary>
    private sealed class WalletScope : IAsyncDisposable
    {
        private readonly GetCodeApiFactory _factory;
        private readonly IServiceScope _serviceScope;
        private readonly bool _ownsCleanup;

        public WalletScope(DatabaseFixture database, bool ownsCleanup = true)
        {
            _ownsCleanup = ownsCleanup;
            _factory = new GetCodeApiFactory(database);
            _serviceScope = _factory.Services.CreateScope();
            Service = _serviceScope.ServiceProvider.GetRequiredService<WalletService>();
        }

        public WalletService Service { get; }

        public async Task<Guid> OpenWalletAsync(Guid userId) =>
            await Service.OpenWalletAsync(new OpenWalletCommand(userId, "USD"), TestContext.Current.CancellationToken);

        public async Task<Guid> WalletIdAsync(Guid userId)
        {
            var wallet = await Db().Wallets.AsNoTracking().FirstAsync(w => w.OwnerUserId == userId, TestContext.Current.CancellationToken);
            return wallet.Id;
        }

        public async Task<IReadOnlyList<LedgerEntry>> EntriesAsync(Guid walletId)
        {
            var context = Db();
            return await context.LedgerEntries
                .AsNoTracking()
                .Where(e => e.WalletId == walletId)
                .OrderBy(e => e.CreatedAtUtc).ThenBy(e => e.Id)
                .ToListAsync(TestContext.Current.CancellationToken);
        }

        public async Task<long> BalanceAsync(Guid walletId) =>
            (await Db().Wallets.AsNoTracking().FirstAsync(w => w.Id == walletId, TestContext.Current.CancellationToken)).BalanceMinor;

        private Persistence.GetCodeDbContext Db() =>
            _serviceScope.ServiceProvider.GetRequiredService<Persistence.GetCodeDbContext>();

        public async ValueTask DisposeAsync()
        {
            if (_ownsCleanup)
            {
                var context = Db();
                await context.LedgerEntries.ExecuteDeleteAsync(CancellationToken.None);
                await context.Wallets.ExecuteDeleteAsync(CancellationToken.None);
                await context.OutboxMessages.Where(m => m.Type.StartsWith("wallet.")).ExecuteDeleteAsync(CancellationToken.None);
            }

            _serviceScope.Dispose();
            await _factory.DisposeAsync();
        }
    }
}
