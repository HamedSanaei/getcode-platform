using GetCode.Application.Providers;

namespace GetCode.Worker;

/// <summary>
/// M04-003: background polling of provider health/balance. Each provider has a
/// "next due" timestamp driven by <see cref="ProviderPollingPolicy"/> (fixed
/// healthy interval, exponential backoff on consecutive faults, capped), so the
/// cadence logic stays unit-tested independently of the hosting loop.
/// </summary>
internal sealed class ProviderHealthPollingWorker(
    ILogger<ProviderHealthPollingWorker> logger,
    ProviderHealthService health,
    IEnumerable<IProviderBalanceObserver> observers) : BackgroundService
{
    internal readonly Dictionary<string, DateTimeOffset> _nextDueUtc = new(StringComparer.Ordinal);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var observer in observers)
        {
            _nextDueUtc[observer.ObserverProviderKey] = now; // first poll immediately
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false))
        {
            foreach (var observer in observers)
            {
                if (_nextDueUtc[observer.ObserverProviderKey] > DateTimeOffset.UtcNow)
                {
                    continue; // backoff/interval gate: not yet due
                }

                try
                {
                    var snapshot = await health.ObserveAsync(observer, stoppingToken).ConfigureAwait(false);
                    _nextDueUtc[observer.ObserverProviderKey] =
                        DateTimeOffset.UtcNow + ProviderPollingPolicy.NextDelay(snapshot.ConsecutiveFailures);
                    logger.LogInformation(
                        "Provider health poll: {ProviderKey} outcome={Outcome} balanceSet={BalanceSet} failures={Failures}",
                        snapshot.ProviderKey, snapshot.Outcome, snapshot.BalanceAmount is not null, snapshot.ConsecutiveFailures);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
            }
        }
    }
}
