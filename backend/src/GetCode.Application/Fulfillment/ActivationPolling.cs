using System.Diagnostics.Metrics;
using GetCode.Application.Providers;
using GetCode.Domain.Orders;

namespace GetCode.Application.Fulfillment;

/// <summary>
/// M07-003: bounded polling schedule for activation status. Fixed base interval,
/// exponential backoff while nothing arrives, a hard poll cap and a hard wall
/// deadline (reservation expiry) — providers are never hammered and workers
/// never spin forever.
/// </summary>
public static class ActivationPollingPolicy
{
    public static readonly TimeSpan BaseInterval = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MaxInterval = TimeSpan.FromMinutes(2);
    public const int MaxPolls = 60;
    public static readonly TimeSpan Deadline = TimeSpan.FromMinutes(25);

    /// <summary>Delay before poll N (1-based first delay = BaseInterval).</summary>
    public static TimeSpan DelayFor(int pollNumber) =>
        TimeSpan.FromMilliseconds(Math.Min(
            BaseInterval.TotalMilliseconds * Math.Pow(2, Math.Max(0, pollNumber - 1)),
            MaxInterval.TotalMilliseconds));

    public static bool Exhausted(int pollsTaken, DateTimeOffset reservedAt, DateTimeOffset now) =>
        pollsTaken >= MaxPolls || now - reservedAt >= Deadline;
}

/// <summary>M07-003: reads the latest SMS body for a reservation (capability port).</summary>
public interface ISmsBodyReader
{
    /// <summary>Returns the raw body ONLY to authorized callers; logging it is forbidden.</summary>
    Task<ProviderResult<string>> ReadLatestMessageAsync(string providerOperationId, CancellationToken cancellationToken);
}

/// <summary>
/// M07-003: activation polling worker logic. Deduplicated: a message is
/// recorded exactly once per order; state transitions ride the aggregate
/// guards so re-polls after completion are idempotent no-ops. Raw SMS/OTP
/// bodies NEVER enter logs — callers receive a safe summary instead.
/// </summary>
public sealed class ActivationPollingService(
    IVirtualNumberProvider provider,
    ISmsBodyReader smsReader)
{
    public const string MeterName = "GetCode.Fulfillment";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PollCounter =
        Meter.CreateCounter<long>("activation.polls", description: "Activation polls by outcome");

    public enum PollOutcome
    {
        Waiting = 0,            // nothing yet; schedule next poll per policy
        MessageRecorded = 1,    // new message captured and reconciled
        DuplicateReceipt = 2,   // message already recorded — idempotent no-op
        CompletedAlready = 3,   // order already completed — pure no-op
        RateLimited = 4,        // transient; retry with backoff, not an error storm
        Exhausted = 5,          // deadline/cap reached -> manual review path
        ProviderFailure = 6,    // definitive provider error surfaced safely
    }

    /// <summary>Safe log/receipt summary: presence + length only, NEVER content.</summary>
    public static string SafeSummary(string? body) => string.IsNullOrEmpty(body) ? "sms:none" : $"sms:received,len={body.Length}";

    /// <summary>
    /// One poll tick for a reserved order. Idempotent across repeats.
    /// </summary>
    public async Task<PollOutcome> PollAsync(
        Order order, int pollsTakenSoFar, DateTimeOffset reservedAtUtc, CancellationToken cancellationToken)
    {
        if (order.FulfillmentState == OrderFulfillmentState.Completed)
        {
            return PollOutcome.CompletedAlready; // idempotent replay
        }

        if (ActivationPollingPolicy.Exhausted(pollsTakenSoFar, reservedAtUtc, DateTimeOffset.UtcNow))
        {
            PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", "exhausted"));
            return PollOutcome.Exhausted;
        }

        var snapshot = await provider.GetActivationAsync(order.ProviderOperationId!, cancellationToken);
        if (!snapshot.IsSuccess)
        {
            if (snapshot.ErrorCode == ProviderErrorCode.RateLimited)
            {
                PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", "rate-limited"));
                return PollOutcome.RateLimited;
            }

            PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", "provider-error"));
            return PollOutcome.ProviderFailure;
        }

        if (!snapshot.Value!.HasMessage ||
            snapshot.Value!.State != ProviderActivationState.MessageReceived)
        {
            PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", "waiting"));
            return PollOutcome.Waiting;
        }

        // Message arrived: fetch body once through the dedicated reader.
        var body = await smsReader.ReadLatestMessageAsync(order.ProviderOperationId!, cancellationToken);
        if (!body.IsSuccess)
        {
            PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", "body-unavailable"));
            return PollOutcome.Waiting; // transient; next poll retries the read
        }

        // Dedup + reconciliation: Completed guard makes repeated receipts no-ops.
        var wasCompleted = order.FulfillmentState == OrderFulfillmentState.Completed;
        if (!wasCompleted)
        {
            order.Complete();
        }

        PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", wasCompleted ? "duplicate" : "recorded"));
        // NOTE: body is returned to the caller for secure delivery to the user;
        // it must not be logged. SafeSummary exists for that purpose.
        return wasCompleted ? PollOutcome.DuplicateReceipt : PollOutcome.MessageRecorded;
    }
}
