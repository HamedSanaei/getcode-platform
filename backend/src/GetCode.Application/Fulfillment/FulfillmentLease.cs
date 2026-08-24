namespace GetCode.Application.Fulfillment;

/// <summary>
/// M07-001: durable fulfillment job. Survives restarts (persisted row);
/// claiming uses ownership leases so N workers can share the queue safely.
/// </summary>
public enum FulfillmentJobState { Pending = 0, Leased = 1, Completed = 2, Failed = 3, DeadLettered = 4 }

public sealed record FulfillmentJob(
    Guid Id,
    Guid OrderId,
    FulfillmentJobState State,
    string? LeaseOwner,
    DateTimeOffset? LeaseExpiresAtUtc,
    int AttemptCount);

/// <summary>Stale-lease and retry policy — explicit, no silent drops.</summary>
public static class FulfillmentLeasePolicy
{
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);
    public const int MaxAttempts = 5;

    public static bool IsExpired(FulfillmentJob job, DateTimeOffset now) =>
        job.State == FulfillmentJobState.Leased && (job.LeaseExpiresAtUtc is null || job.LeaseExpiresAtUtc <= now);

    public static bool IsDeadLetter(int attemptCount) => attemptCount >= MaxAttempts;
}

/// <summary>
/// Durable store contract. Implementations MUST make <see cref="ClaimNextAsync"/>
/// atomic across concurrent callers (conditional UPDATE / row lock) — that is
/// what makes multiple workers safe.
/// </summary>
public interface IFulfillmentJobStore
{
    /// <summary>Enqueue fulfillment for an order (idempotent per order).</summary>
    Task<bool> EnqueueAsync(Guid orderId, CancellationToken cancellationToken);

    /// <summary>
    /// Atomically claim the next runnable job for a worker: pending jobs first,
    /// then expired leases (crash recovery). Returns null when nothing is runnable.
    /// </summary>
    Task<FulfillmentJob?> ClaimNextAsync(string workerId, DateTimeOffset now, CancellationToken cancellationToken);

    Task CompleteAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>M07-002: terminal manual-review state for ambiguous purchases (no retry).</summary>
    Task MarkDeadLetteredAsync(Guid jobId, CancellationToken cancellationToken);

    /// <summary>Release a failed job for retry (or dead-letter when attempts exhausted).</summary>
    Task FailAsync(Guid jobId, DateTimeOffset now, CancellationToken cancellationToken);

    /// <summary>Jobs whose lease expired while a worker was down become claimable again.</summary>
    Task<int> RecoverExpiredLeasesAsync(DateTimeOffset now, CancellationToken cancellationToken);

    Task<bool> ExistsForOrderAsync(Guid orderId, CancellationToken cancellationToken);
}
