using GetCode.Domain.Orders;

namespace GetCode.Application.Payments;

/// <summary>M06-005: the durable fact that an order was paid.</summary>
public sealed record OrderPaidEvent(Guid OrderId, decimal Amount, string Currency, DateTimeOffset PaidAtUtc);

/// <summary>
/// M06-005: atomic commit port — implementations must persist the order's
/// payment-state transition AND the outbox intent in ONE database transaction,
/// so a crash can never yield a paid order without its fulfillment intent
/// (or vice versa).
/// </summary>
public interface IOrderPaidUnitOfWork
{
    Task CommitAsync(Order order, OrderPaidEvent paidEvent, CancellationToken cancellationToken);
}

/// <summary>Consumer of dispatched events; MUST be idempotent per messageId. Returns false when already handled.</summary>
public interface IOutboxDispatchHandler
{
    Task<bool> HandleOnceAsync(Guid messageId, OrderPaidEvent paidEvent, CancellationToken cancellationToken);
}

/// <summary>
/// M06-005: explicit retry/dead-letter policy. Attempts are counted per message;
/// exponential backoff between attempts; after <see cref="MaxAttempts"/> the
/// message goes to manual review (dead letter) — never silently dropped.
/// </summary>
public static class OutboxRetryPolicy
{
    public const int MaxAttempts = 5;

    public static bool IsDeadLetter(int attemptCount) => attemptCount >= MaxAttempts;

    /// <summary>Delay before retry N (1-based): 30s, 60s, 120s, 240s… capped at 15min.</summary>
    public static TimeSpan RetryDelay(int attemptNumber) =>
        TimeSpan.FromMilliseconds(Math.Min(30_000d * Math.Pow(2, Math.Max(0, attemptNumber - 1)), 15 * 60_000d));
}

/// <summary>A claimed job handed to the worker loop.</summary>
public sealed record OutboxClaim(Guid MessageId, OrderPaidEvent Event, int AttemptCount);
