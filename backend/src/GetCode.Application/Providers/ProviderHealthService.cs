using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace GetCode.Application.Providers;

/// <summary>
/// M04-003: provider health/balance observation. Polls every registered
/// <see cref="IProviderBalanceObserver"/>, keeps the latest timestamped,
/// normalized snapshot per provider, and applies the shared
/// <see cref="ProviderPollingPolicy"/> backoff on faults. Balance data is
/// supplier telemetry — never customer wallet truth (AGENTS.md money rules).
/// <para>Metrics: polls are counted on the `GetCode.ProviderHealth` meter with
/// an outcome attribute; the hosting worker adds structured logs.</para>
/// </summary>
public sealed class ProviderHealthService
{
    public const string MeterName = "GetCode.ProviderHealth";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> PollCounter =
        Meter.CreateCounter<long>("provider.health.polls", description: "Provider health/balance poll attempts");

    private readonly ConcurrentDictionary<string, ProviderHealthSnapshot> _latest = new(StringComparer.Ordinal);

    /// <summary>Latest snapshot per observed provider (read model for ops UI).</summary>
    public IReadOnlyList<ProviderHealthSnapshot> LatestSnapshots => [.. _latest.Values.OrderBy(s => s.ProviderKey)];

    /// <summary>Observes one provider now; returns the stored snapshot.</summary>
    public async Task<ProviderHealthSnapshot> ObserveAsync(
        IProviderBalanceObserver observer, CancellationToken cancellationToken)
    {
        var sw = ValueStopwatch.StartNew();
        ProviderHealthSnapshot snapshot;
        try
        {
            var result = await observer.ObserveBalanceAsync(cancellationToken);
            snapshot = result.IsSuccess
                ? new ProviderHealthSnapshot(
                    observer.ObserverProviderKey,
                    ProviderObservationOutcome.Healthy,
                    result.Value,
                    SafeErrorToken: null,
                    ConsecutiveFailures: 0,
                    ObservedAtUtc: DateTimeOffset.UtcNow)
                : FaultSnapshot(observer.ObserverProviderKey, _latest, result.ErrorCode.ToString(), result.SafeErrorCode);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Adapters should not throw, but a faulting adapter must never kill polling.
            snapshot = FaultSnapshot(observer.ObserverProviderKey, _latest, ex.GetType().Name, "observer-exception");
        }

        PollCounter.Add(1, new KeyValuePair<string, object?>("outcome", snapshot.Outcome.ToString()));
        _latest[snapshot.ProviderKey] = snapshot;
        return snapshot;
    }

    private static ProviderHealthSnapshot FaultSnapshot(
        string providerKey, ConcurrentDictionary<string, ProviderHealthSnapshot> latest,
        string fallbackToken, string? safeToken)
    {
        var failures = latest.TryGetValue(providerKey, out var prior) ? prior.ConsecutiveFailures + 1 : 1;
        return new ProviderHealthSnapshot(
            providerKey,
            failures >= 3 ? ProviderObservationOutcome.Unreachable : ProviderObservationOutcome.Degraded,
            BalanceAmount: null,
            SafeErrorToken: safeToken ?? fallbackToken,
            ConsecutiveFailures: failures,
            ObservedAtUtc: DateTimeOffset.UtcNow);
    }
}

/// <summary>
/// M04-003: shared polling schedule — fixed healthy interval with exponential
/// backoff on consecutive failures (capped). Pure logic so worker scheduling is
/// unit-testable without hosting a BackgroundService.
/// </summary>
public static class ProviderPollingPolicy
{
    public static readonly TimeSpan HealthyInterval = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan MaxBackoff = TimeSpan.FromMinutes(15);

    /// <summary>Delay before the next poll given the provider's current failure streak.</summary>
    public static TimeSpan NextDelay(int consecutiveFailures)
    {
        if (consecutiveFailures <= 0)
        {
            return HealthyInterval;
        }

        // 60s * 2^failures, capped at MaxBackoff.
        var exponent = Math.Min(consecutiveFailures, 8); // guard against overflow
        var backoffMs = (double)HealthyInterval.TotalMilliseconds * Math.Pow(2, exponent);
        return TimeSpan.FromMilliseconds(Math.Min(backoffMs, MaxBackoff.TotalMilliseconds));
    }
}

/// <summary>Minimal allocation stopwatch helper (no dependency on Stopwatch type in hot path).</summary>
internal readonly struct ValueStopwatch
{
    private readonly long _startTimestamp;
    private ValueStopwatch(long startTimestamp) => _startTimestamp = startTimestamp;
    public static ValueStopwatch StartNew() => new(System.Diagnostics.Stopwatch.GetTimestamp());
    public long ElapsedMilliseconds => (long)((System.Diagnostics.Stopwatch.GetTimestamp() - _startTimestamp) * 1000d / System.Diagnostics.Stopwatch.Frequency);
}
