using GetCode.Application.Fulfillment;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M07-001 verification: durable fulfillment leases — concurrent workers claim
/// disjoint jobs, jobs survive restarts (persisted rows), and stale leases are
/// recovered without losing or duplicating work.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class FulfillmentLeaseTests(DatabaseFixture database)
{
    private static IFulfillmentJobStore NewStore(GetCodeApiFactory factory) =>
        factory.Services.CreateScope().ServiceProvider.GetRequiredService<IFulfillmentJobStore>();

    [Fact]
    public async Task Concurrent_workers_claim_disjoint_jobs_never_the_same_one()
    {
        await using var factory = new GetCodeApiFactory(database);
        var seeder = NewStore(factory);
        var orderIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        foreach (var id in orderIds)
        {
            Assert.True(await seeder.EnqueueAsync(id, TestContext.Current.CancellationToken));
        }

        // Two "workers" with independent scopes race for the queue.
        using var scopeA = factory.Services.CreateScope();
        using var scopeB = factory.Services.CreateScope();
        var workerA = scopeA.ServiceProvider.GetRequiredService<IFulfillmentJobStore>();
        var workerB = scopeB.ServiceProvider.GetRequiredService<IFulfillmentJobStore>();

        var claims = await Task.WhenAll(
            workerA.ClaimNextAsync("worker-a", DateTimeOffset.UtcNow, TestContext.Current.CancellationToken),
            workerB.ClaimNextAsync("worker-b", DateTimeOffset.UtcNow, TestContext.Current.CancellationToken));

        Assert.NotNull(claims[0]);
        Assert.NotNull(claims[1]);
        Assert.NotEqual(claims[0]!.Id, claims[1]!.Id);                       // never the same job
        Assert.All(claims, c => Assert.Equal(FulfillmentJobState.Leased, c!.State));
        Assert.NotEqual(claims[0]!.LeaseOwner, claims[1]!.LeaseOwner);
    }

    [Fact]
    public async Task Queue_drains_to_null_and_never_repeats_a_job()
    {
        await using var factory = new GetCodeApiFactory(database);
        var store = NewStore(factory);
        var orderId = Guid.NewGuid();
        await store.EnqueueAsync(orderId, TestContext.Current.CancellationToken);

        // Drain (the shared database may hold leftovers from sibling tests):
        // every claim is distinct and eventually the queue yields null.
        var seen = new HashSet<Guid>();
        for (var i = 0; i < 50; i++)
        {
            var claim = await store.ClaimNextAsync($"drain-worker-{i}", DateTimeOffset.UtcNow, TestContext.Current.CancellationToken);
            if (claim is null)
            {
                return; // exhausted
            }

            Assert.True(seen.Add(claim.Id), $"job {claim.Id} claimed twice"); // never duplicate ownership
        }

        Assert.Fail("queue did not drain within bounds");
    }

    [Fact]
    public async Task Job_survives_restart_and_stale_lease_is_recovered_without_loss()
    {
        await using var factory = new GetCodeApiFactory(database);
        var orderId = Guid.NewGuid();
        var store = NewStore(factory);
        Assert.True(await store.EnqueueAsync(orderId, TestContext.Current.CancellationToken));

        // Worker claims, then "crashes": no complete/fail call ever happens.
        // (Sibling tests may leave other jobs pending, so claim until OURS.)
        FulfillmentJob? claimed = null;
        for (var i = 0; i < 20 && claimed is null; i++)
        {
            claimed = await store.ClaimNextAsync($"doomed-{i}", DateTimeOffset.UtcNow, TestContext.Current.CancellationToken);
            if (claimed?.OrderId != orderId)
            {
                claimed = null; // not ours; keep looking (its lease will expire harmlessly)
            }
        }

        Assert.NotNull(claimed);
        factory.Dispose(); // simulate container death

        // Restart: fresh factory over the same PostgreSQL.
        await using var restarted = new GetCodeApiFactory(database);
        var revived = NewStore(restarted);

        // Once the lease expires, recovery makes expired jobs claimable again
        // (count may include leftovers from sibling tests — that is fine).
        var afterExpiry = DateTimeOffset.UtcNow.AddMinutes(3);
        var recovered = await revived.RecoverExpiredLeasesAsync(afterExpiry, TestContext.Current.CancellationToken);
        Assert.True(recovered >= 1, "expired lease was not recovered");

        // Our crashed job must be reclaimable by a new owner — no lost work.
        FulfillmentJob? reclaimed = null;
        for (var i = 0; i < 20 && reclaimed is null; i++)
        {
            var claim = await revived.ClaimNextAsync($"recovery-worker-{i}", afterExpiry, TestContext.Current.CancellationToken);
            if (claim?.OrderId == orderId)
            {
                reclaimed = claim;
            }
        }

        Assert.NotNull(reclaimed);                                              // no lost work
        Assert.StartsWith("recovery-worker-", reclaimed!.LeaseOwner);           // new owner won it
    }

    [Fact]
    public async Task Enqueue_is_idempotent_per_order()
    {
        await using var factory = new GetCodeApiFactory(database);
        var store = NewStore(factory);
        var orderId = Guid.NewGuid();

        Assert.True(await store.EnqueueAsync(orderId, TestContext.Current.CancellationToken));
        Assert.False(await store.EnqueueAsync(orderId, TestContext.Current.CancellationToken)); // duplicate refused
        Assert.True(await store.ExistsForOrderAsync(orderId, TestContext.Current.CancellationToken));
    }
}
