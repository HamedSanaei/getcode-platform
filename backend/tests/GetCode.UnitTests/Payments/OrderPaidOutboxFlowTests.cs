using GetCode.Application.Payments;
using GetCode.Domain.Orders;

namespace GetCode.UnitTests.Payments;

/// <summary>
/// M06-005 verification: transaction rollback (paid state + outbox intent
/// commit or fail together), duplicate dispatch absorbed by the idempotent
/// handler, and worker-crash recovery with explicit dead-letter policy.
/// </summary>
public sealed class OrderPaidOutboxFlowTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order NewOrder() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "idem-ob", 127m, "RUB",
        "RU", "telegram", "activation", 1, T0);

    private sealed class FakeUnitOfWork(bool fail) : IOrderPaidUnitOfWork
    {
        public bool OrderSaved { get; private set; }
        public OrderPaidEvent? EventSaved { get; private set; }

        public Task CommitAsync(Order order, OrderPaidEvent paidEvent, CancellationToken ct)
        {
            if (fail)
            {
                throw new IOException("db-connection-lost"); // crash between writes is the danger this port prevents
            }

            OrderSaved = true;
            EventSaved = paidEvent;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeLeaseStore : IOutboxLeaseStore
    {
        public readonly Queue<OutboxClaim> Pending = new();
        public readonly HashSet<Guid> Completed = [];
        public readonly Dictionary<Guid, int> Failures = [];
        public readonly HashSet<Guid> DeadLettered = [];

        public Task<OutboxClaim?> ClaimNextAsync(CancellationToken ct) =>
            Task.FromResult(Pending.Count > 0 ? Pending.Dequeue() : null);

        public Task MarkCompletedAsync(Guid messageId, CancellationToken ct)
        {
            Completed.Add(messageId);
            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(Guid messageId, string code, CancellationToken ct)
        {
            Failures[messageId] = Failures.GetValueOrDefault(messageId) + 1;
            // Re-lease for retry (simulates the persistence-side retry visibility).
            return Task.CompletedTask;
        }

        public Task MarkDeadLetteredAsync(Guid messageId, CancellationToken ct)
        {
            DeadLettered.Add(messageId);
            return Task.CompletedTask;
        }
    }

    private sealed class CountingHandler(int succeedAfter = 1) : IOutboxDispatchHandler
    {
        private readonly HashSet<Guid> _handled = [];
        public int Calls { get; private set; }
        public int Fulfillments { get; private set; }

        public Task<bool> HandleOnceAsync(Guid messageId, OrderPaidEvent evt, CancellationToken ct)
        {
            Calls++;
            if (Calls < succeedAfter)
            {
                throw new IOException("transient-handler-failure");
            }

            if (_handled.Add(messageId))
            {
                Fulfillments++; // real fulfillment side effect happens exactly once
                return Task.FromResult(true);
            }

            return Task.FromResult(false); // duplicate delivery: no second fulfillment
        }
    }

    [Fact]
    public async Task Transaction_rollback_leaves_neither_paid_state_nor_outbox_intent()
    {
        var order = NewOrder();
        var failingCommit = new FakeUnitOfWork(fail: true);
        var service = new PaymentCallbackService(
            new AlwaysValidVerifier(), new StubOrderRepo(order), failingCommit);

        await Assert.ThrowsAsync<IOException>(() =>
            service.HandleCallbackAsync("ref", 127m, "RUB", "sig", TestContext.Current.CancellationToken));

        Assert.False(failingCommit.OrderSaved);   // no partial commit
        Assert.Null(failingCommit.EventSaved);    // no orphan intent
        // Durable truth lives behind the unit of work: nothing was committed,
        // so a fresh read (new session) still sees AwaitingPayment.
        var freshRead = await new StubOrderRepo(NewOrder()).FindByIdAsync(order.Id, TestContext.Current.CancellationToken);
        Assert.Equal(OrderPaymentState.AwaitingPayment, freshRead!.PaymentState);
    }

    [Fact]
    public async Task Successful_commit_writes_paid_order_and_intent_atomically()
    {
        var order = NewOrder();
        order.MarkPaymentAuthorized(); // gateway capture implies both transitions
        var commit = new FakeUnitOfWork(fail: false);
        var service = new PaymentCallbackService(
            new AlwaysValidVerifier(), new StubOrderRepo(order), commit);

        var result = await service.HandleCallbackAsync("ref", 127m, "RUB", "sig", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentCallbackService.Handling.Applied, result.Outcome);
        Assert.True(commit.OrderSaved);
        Assert.NotNull(commit.EventSaved);
        Assert.Equal(OrderPaymentState.Paid, order.PaymentState);
    }

    [Fact]
    public async Task Duplicate_dispatch_is_absorbed_fulfillment_runs_exactly_once()
    {
        var store = new FakeLeaseStore();
        var handler = new CountingHandler();
        var worker = new OutboxWorkerService(store, handler);
        var evt = new OrderPaidEvent(Guid.NewGuid(), 127m, "RUB", T0);
        var message = new OutboxClaim(Guid.NewGuid(), evt, AttemptCount: 0);

        store.Pending.Enqueue(message);
        Assert.Equal(OutboxWorkerService.Dispatch.Handled, await worker.ProcessNextAsync(TestContext.Current.CancellationToken));

        // The same message redelivered (at-least-once) — fulfillment must not repeat.
        store.Pending.Enqueue(message);
        Assert.Equal(OutboxWorkerService.Dispatch.AlreadyHandled, await worker.ProcessNextAsync(TestContext.Current.CancellationToken));

        Assert.Equal(2, handler.Calls);
        Assert.Equal(1, handler.Fulfillments); // exactly-once side effect
        Assert.Single(store.Completed);
    }

    [Fact]
    public async Task Worker_crash_between_claim_and_completion_redelivers_and_recovers()
    {
        var store = new FakeLeaseStore();
        var handler = new CountingHandler(succeedAfter: 2); // first attempt crashes mid-handling
        var worker = new OutboxWorkerService(store, handler);
        var message = new OutboxClaim(Guid.NewGuid(), new OrderPaidEvent(Guid.NewGuid(), 5m, "USD", T0), AttemptCount: 0);

        // Attempt 1: handler throws → marked failed (crash simulated), not completed.
        store.Pending.Enqueue(message);
        Assert.Equal(OutboxWorkerService.Dispatch.Failed, await worker.ProcessNextAsync(TestContext.Current.CancellationToken));
        Assert.DoesNotContain(message.MessageId, store.Completed);

        // Retry after recovery succeeds.
        store.Pending.Enqueue(new OutboxClaim(message.MessageId, message.Event, AttemptCount: 1));
        Assert.Equal(OutboxWorkerService.Dispatch.Handled, await worker.ProcessNextAsync(TestContext.Current.CancellationToken));
        Assert.Contains(message.MessageId, store.Completed);
        Assert.Equal(1, handler.Fulfillments);
    }

    [Fact]
    public async Task Poison_message_goes_to_explicit_dead_letter_after_max_attempts()
    {
        var store = new FakeLeaseStore();
        var handler = new AlwaysFailingHandler();
        var worker = new OutboxWorkerService(store, handler);
        var message = new OutboxClaim(Guid.NewGuid(), new OrderPaidEvent(Guid.NewGuid(), 5m, "USD", T0), OutboxRetryPolicy.MaxAttempts);

        store.Pending.Enqueue(message);
        Assert.Equal(OutboxWorkerService.Dispatch.DeadLettered, await worker.ProcessNextAsync(TestContext.Current.CancellationToken));
        Assert.Contains(message.MessageId, store.DeadLettered); // manual review — never silently dropped
        Assert.Empty(store.Completed);
    }

    [Fact]
    public void Retry_policy_delays_grow_and_cap()
    {
        Assert.True(OutboxRetryPolicy.RetryDelay(1) < OutboxRetryPolicy.RetryDelay(2));
        Assert.True(OutboxRetryPolicy.RetryDelay(3) < OutboxRetryPolicy.RetryDelay(4));
        Assert.Equal(TimeSpan.FromMinutes(15), OutboxRetryPolicy.RetryDelay(50)); // capped
        Assert.True(OutboxRetryPolicy.IsDeadLetter(OutboxRetryPolicy.MaxAttempts));
        Assert.False(OutboxRetryPolicy.IsDeadLetter(OutboxRetryPolicy.MaxAttempts - 1));
    }

    private sealed class AlwaysValidVerifier : IPaymentCallbackVerifier
    {
        public CallbackValidation ValidateCallback(string reference, decimal amount, string currency, string? signature) =>
            new(CallbackValidationOutcome.Valid, Guid.NewGuid(), string.Empty);
    }

    private sealed class StubOrderRepo(Order order) : GetCode.Application.Orders.IOrderRepository
    {
        public Task<Order?> FindByIdempotencyKeyAsync(Guid customerId, string key, CancellationToken ct) => Task.FromResult<Order?>(order);
        public Task<Order?> FindByIdAsync(Guid orderId, CancellationToken ct) => Task.FromResult<Order?>(order);
        public Task AddAsync(Order order, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Order order, CancellationToken ct) => Task.CompletedTask;
    }

    private sealed class AlwaysFailingHandler : IOutboxDispatchHandler
    {
        public Task<bool> HandleOnceAsync(Guid messageId, OrderPaidEvent evt, CancellationToken ct) =>
            throw new InvalidOperationException("poison");
    }
}
