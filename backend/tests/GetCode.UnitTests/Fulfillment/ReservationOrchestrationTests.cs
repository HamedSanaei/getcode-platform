using GetCode.Application.Fulfillment;
using GetCode.Application.Providers;
using GetCode.Application.Orders;
using GetCode.Domain.Orders;

namespace GetCode.UnitTests.Fulfillment;

/// <summary>
/// M07-002: reservation orchestration — success reconciles into the order
/// atomically-completing the job; ambiguous results dead-letter WITHOUT any
/// further provider contact; definitive failures stay retryable.
/// </summary>
public sealed class ReservationOrchestrationTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class MemoryStore : IFulfillmentJobStore
    {
        public readonly Dictionary<Guid, FulfillmentJob> Jobs = [];
        public int ProviderContactAttempts { get; set; }

        public Task<bool> EnqueueAsync(Guid orderId, CancellationToken ct)
        {
            var job = new FulfillmentJob(Guid.CreateVersion7(), orderId, FulfillmentJobState.Pending, null, null, 0);
            Jobs[job.Id] = job;
            return Task.FromResult(true);
        }

        public Task<FulfillmentJob?> ClaimNextAsync(string workerId, DateTimeOffset now, CancellationToken ct) =>
            Task.FromResult(Jobs.Values.FirstOrDefault(j => j.State == FulfillmentJobState.Pending));

        public Task CompleteAsync(Guid jobId, CancellationToken ct)
        {
            Jobs[jobId] = Jobs[jobId] with { State = FulfillmentJobState.Completed };
            return Task.CompletedTask;
        }

        public Task MarkDeadLetteredAsync(Guid jobId, CancellationToken ct)
        {
            Jobs[jobId] = Jobs[jobId] with { State = FulfillmentJobState.DeadLettered };
            return Task.CompletedTask;
        }

        public Task FailAsync(Guid jobId, DateTimeOffset now, CancellationToken ct)
        {
            var job = Jobs[jobId];
            Jobs[jobId] = job with
            {
                State = FulfillmentLeasePolicy.IsDeadLetter(job.AttemptCount + 1)
                    ? FulfillmentJobState.DeadLettered : FulfillmentJobState.Pending,
                AttemptCount = job.AttemptCount + 1,
            };
            return Task.CompletedTask;
        }

        public Task<int> RecoverExpiredLeasesAsync(DateTimeOffset now, CancellationToken ct) => Task.FromResult(0);
        public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken ct) =>
            Task.FromResult(Jobs.Values.Any(j => j.OrderId == orderId));
    }

    private sealed class MemoryOrderRepo(Order order) : IOrderRepository
    {
        public Order Current { get; } = order;
        public int Saves { get; private set; }
        public Task<Order?> FindByIdAsync(Guid id, CancellationToken ct) => Task.FromResult<Order?>(Current);
        public Task<Order?> FindByIdempotencyKeyAsync(Guid customerId, string key, CancellationToken ct) => Task.FromResult<Order?>(Current);
        public Task AddAsync(Order order, CancellationToken ct) => Task.CompletedTask;
        public Task UpdateAsync(Order order, CancellationToken ct) { Saves++; return Task.CompletedTask; }
    }

    [Fact]
    public async Task Successful_reservation_reconciles_order_and_completes_job()
    {
        var order = PaidOrder();
        var store = new MemoryStore();
        await store.EnqueueAsync(order.Id, TestContext.Current.CancellationToken);
        var job = (await store.ClaimNextAsync("w", T0, TestContext.Current.CancellationToken))!;
        var repo = new MemoryOrderRepo(order);
        var orchestrator = new ProviderReservationOrchestrator((_, _) =>
            ProviderResult<ProviderReservation>.Success(new ProviderReservation("op-77", "+79123456789", T0, null)));
        var service = new ReservationOrchestrationService(store, repo, orchestrator);

        var outcome = await service.ProcessAsync(job, Query(), "offer", ["five-sim"], TestContext.Current.CancellationToken);

        Assert.Equal(ReservationOrchestrationService.Outcome.Reserved, outcome);
        Assert.Equal("op-77", order.ProviderOperationId);
        Assert.Equal(OrderFulfillmentState.Reserved, order.FulfillmentState);
        Assert.Equal(FulfillmentJobState.Completed, store.Jobs[job.Id].State);
    }

    [Fact]
    public async Task Ambiguous_result_dead_letters_and_never_lets_a_retry_contact_providers()
    {
        var order = PaidOrder();
        var store = new MemoryStore();
        await store.EnqueueAsync(order.Id, TestContext.Current.CancellationToken);
        var job = (await store.ClaimNextAsync("w", T0, TestContext.Current.CancellationToken))!;
        var repo = new MemoryOrderRepo(order);
        var providerCalls = 0;
        var orchestrator = new ProviderReservationOrchestrator((_, _) =>
        {
            providerCalls++;
            return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase");
        });
        var service = new ReservationOrchestrationService(store, repo, orchestrator);

        var first = await service.ProcessAsync(job, Query(), "offer", ["five-sim"], TestContext.Current.CancellationToken);
        // Simulate a worker restart re-running the SAME job: the blocked key must
        // prevent ANY further provider contact and keep it in manual review.
        var second = await service.ProcessAsync(job, Query(), "offer", ["five-sim"], TestContext.Current.CancellationToken);

        Assert.Equal(ReservationOrchestrationService.Outcome.AmbiguousManualReview, first);
        Assert.Equal(ReservationOrchestrationService.Outcome.AmbiguousManualReview, second);
        Assert.Equal(1, providerCalls); // exactly ONE provider contact ever
        Assert.Equal(FulfillmentJobState.DeadLettered, store.Jobs[job.Id].State);
        Assert.Null(order.ProviderOperationId); // no phantom reservation recorded
    }

    [Fact]
    public async Task Definitive_failure_stays_retryable_without_blocking()
    {
        var order = PaidOrder();
        var store = new MemoryStore();
        await store.EnqueueAsync(order.Id, TestContext.Current.CancellationToken);
        var job = (await store.ClaimNextAsync("w", T0, TestContext.Current.CancellationToken))!;
        var calls = 0;
        var orchestrator = new ProviderReservationOrchestrator((_, _) =>
        {
            calls++;
            if (calls <= 2) // both candidates definitively out of stock on attempt 1
            {
                return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.OfferUnavailable, "no-inventory");
            }

            return ProviderResult<ProviderReservation>.Success(new ProviderReservation("op-2", "+79009999999", T0, null));
        });
        var service = new ReservationOrchestrationService(store, repo(order), orchestrator);

        var first = await service.ProcessAsync(job, Query(), "offer", ["a", "b"], TestContext.Current.CancellationToken);
        Assert.Equal(ReservationOrchestrationService.Outcome.RetryableFailure, first);
        Assert.Equal(FulfillmentJobState.Pending, store.Jobs[job.Id].State); // back in queue

        var second = await service.ProcessAsync(
            store.Jobs[job.Id] with { AttemptCount = 1 }, Query(), "offer", ["a", "b"], TestContext.Current.CancellationToken);
        Assert.Equal(ReservationOrchestrationService.Outcome.Reserved, second); // retry succeeded
        Assert.Equal("op-2", order.ProviderOperationId);

        static MemoryOrderRepo repo(Order o) => new(o);
    }

    private static Order PaidOrder()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "idem-orch", 127m, "RUB",
            "RU", "telegram", "activation", 1, T0);
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        return order;
    }

    private static ProviderSearchQuery Query() => new("RU", "telegram", "activation");
}
