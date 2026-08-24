using GetCode.Application.Fulfillment;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Fulfillment;

/// <summary>EF-mapped durable job row (M07-001).</summary>
public sealed class FulfillmentRequestRecord
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public FulfillmentJobState State { get; set; } = FulfillmentJobState.Pending;
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>
/// M07-001: PostgreSQL-backed store. Claiming uses a single conditional
/// UPDATE … WHERE … RETURNING inside one statement, which is atomic across
/// concurrent workers — two workers can never own the same job at once.
/// </summary>
public sealed class FulfillmentJobStore(GetCodeDbContext db) : IFulfillmentJobStore
{
    public async Task<bool> EnqueueAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var exists = await db.Set<FulfillmentRequestRecord>().AnyAsync(r => r.OrderId == orderId, cancellationToken);
        if (exists)
        {
            return false; // idempotent enqueue
        }

        db.Add(new FulfillmentRequestRecord
        {
            Id = Guid.CreateVersion7(),
            OrderId = orderId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken) =>
        db.Set<FulfillmentRequestRecord>().AnyAsync(r => r.OrderId == orderId, cancellationToken);

    public async Task<FulfillmentJob?> ClaimNextAsync(string workerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var leaseEnd = now + FulfillmentLeasePolicy.LeaseDuration;
        var claimed = await db.Database.ExecuteSqlRawAsync("""
            UPDATE fulfillment_requests fr
            SET state = {1}, lease_owner = {0}, lease_expires_at_utc = {2}
            WHERE fr.id = (
                SELECT id FROM fulfillment_requests
                WHERE (state = {3})
                   OR (state = {1} AND lease_expires_at_utc <= {4})
                ORDER BY created_at_utc
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            RETURNING id, order_id, attempt_count;
            """, new object[] { workerId, (int)FulfillmentJobState.Leased, leaseEnd,
                 (int)FulfillmentJobState.Pending, now, (int)FulfillmentJobState.Leased },
            cancellationToken);

        if (claimed == 0)
        {
            return null;
        }

        // Read back the row we just won (single-row by unique owner+expiry is unambiguous).
        var record = await db.Set<FulfillmentRequestRecord>()
            .AsNoTracking()
            .SingleAsync(r => r.LeaseOwner == workerId && r.State == FulfillmentJobState.Leased, cancellationToken);
        return new FulfillmentJob(record.Id, record.OrderId, record.State, workerId, record.LeaseExpiresAtUtc, record.AttemptCount);
    }

    public async Task CompleteAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var record = await db.Set<FulfillmentRequestRecord>().SingleAsync(r => r.Id == jobId, cancellationToken);
        record.State = FulfillmentJobState.Completed;
        record.LeaseOwner = null;
        record.LeaseExpiresAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkDeadLetteredAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var record = await db.Set<FulfillmentRequestRecord>().SingleAsync(r => r.Id == jobId, cancellationToken);
        record.State = FulfillmentJobState.DeadLettered;
        record.LeaseOwner = null;
        record.LeaseExpiresAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task FailAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var record = await db.Set<FulfillmentRequestRecord>().SingleAsync(r => r.Id == jobId, cancellationToken);
        record.AttemptCount++;
        if (FulfillmentLeasePolicy.IsDeadLetter(record.AttemptCount))
        {
            record.State = FulfillmentJobState.DeadLettered;
        }
        else
        {
            record.State = FulfillmentJobState.Pending;
        }

        record.LeaseOwner = null;
        record.LeaseExpiresAtUtc = null;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RecoverExpiredLeasesAsync(DateTimeOffset now, CancellationToken cancellationToken) =>
        await db.Set<FulfillmentRequestRecord>()
            .Where(r => r.State == FulfillmentJobState.Leased && r.LeaseExpiresAtUtc <= now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.State, FulfillmentJobState.Pending)
                .SetProperty(r => r.LeaseOwner, (string?)null)
                .SetProperty(r => r.LeaseExpiresAtUtc, (DateTimeOffset?)null), cancellationToken);
}
