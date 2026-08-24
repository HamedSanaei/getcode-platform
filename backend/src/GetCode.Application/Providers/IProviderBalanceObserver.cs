namespace GetCode.Application.Providers;

/// <summary>
/// M04-003: capability port for observing a provider account's operational
/// balance. Implemented by adapters that can query it (5SIM). Deliberately
/// separate from <see cref="IVirtualNumberProvider"/>: not every provider
/// exposes balance, and this data is NEVER customer wallet truth — it is
/// supplier telemetry used for health monitoring and low-balance alerts.
/// </summary>
public interface IProviderBalanceObserver
{
    string ObserverProviderKey { get; }

    Task<ProviderResult<decimal>> ObserveBalanceAsync(CancellationToken cancellationToken);
}

/// <summary>Normalized, timestamped health observation for one provider.</summary>
public sealed record ProviderHealthSnapshot(
    string ProviderKey,
    ProviderObservationOutcome Outcome,
    decimal? BalanceAmount,
    string? SafeErrorToken,
    int ConsecutiveFailures,
    DateTimeOffset ObservedAtUtc);

public enum ProviderObservationOutcome : byte
{
    Healthy = 0,
    Degraded = 1,
    Unreachable = 2,
}
