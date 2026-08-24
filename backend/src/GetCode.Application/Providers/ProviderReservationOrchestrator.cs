using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace GetCode.Application.Providers;

/// <summary>
/// M04-006: safe failover and ambiguous-outcome reconciliation for provider
/// reservations. Wraps candidate attempts produced by the M04-005 policy with
/// a strict attempt-state machine:
/// <list type="bullet">
/// <item>definitely-not-applied → failover to the next candidate is safe;</item>
/// <item>applied → done;</item>
/// <item>ambiguous → STOP. No blind retry, no failover. The case enters
/// Manual Review until reconciled (v1 keeps entries in-process; durable
/// storage lands with the M06 order flow).</item>
/// </list>
/// Duplicate-purchase prevention: an idempotency key that ever became
/// ambiguous is remembered until resolved and refuses further attempts.
/// </summary>
public sealed class ProviderReservationOrchestrator
{
    public const string MeterName = "GetCode.ProviderReservation";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> OutcomeCounter =
        Meter.CreateCounter<long>("provider.reservation.attempts", description: "Reservation attempt outcomes");

    public enum AttemptState { DefinitelyNotApplied = 0, Applied = 1, Ambiguous = 2 }

    public sealed record AttemptResult(
        AttemptState State,
        string? SelectedProviderKey,
        ProviderReservation? Reservation,
        string ReasonToken,
        IReadOnlyList<(string ProviderKey, string Outcome)> Trail);

    public enum ReconciliationState { PendingManualReview = 0, ResolvedNotApplied = 1 }

    public sealed record ReconciliationEntry(
        string IdempotencyKey,
        string ProviderKey,
        ReconciliationState State,
        DateTimeOffset CreatedAtUtc);

    private readonly Func<ProviderSearchQuery?, string?, ProviderResult<ProviderReservation>> _attemptFactory;
    private readonly ConcurrentDictionary<string, ReconciliationEntry> _reconciliations = new(StringComparer.Ordinal);

    public ProviderReservationOrchestrator(Func<ProviderSearchQuery?, string?, ProviderResult<ProviderReservation>> attemptFactory) =>
        _attemptFactory = attemptFactory;

    /// <summary>Ambiguous keys awaiting or past review — duplicate-charge guard.</summary>
    public IReadOnlyList<ReconciliationEntry> PendingReconciliations => [.. _reconciliations.Values.OrderBy(e => e.CreatedAtUtc)];

    /// <summary>True while the key must refuse new purchase attempts (only pending review blocks).</summary>
    public bool IsBlockedFor(string idempotencyKey) =>
        _reconciliations.TryGetValue(idempotencyKey, out var entry) &&
        entry.State == ReconciliationState.PendingManualReview;

    /// <summary>Ops resolution: evidence showed the purchase did not happen.</summary>
    public bool ResolveNotApplied(string idempotencyKey)
    {
        if (!_reconciliations.TryGetValue(idempotencyKey, out var entry) ||
            entry.State != ReconciliationState.PendingManualReview)
        {
            return false;
        }

        _reconciliations[idempotencyKey] = entry with { State = ReconciliationState.ResolvedNotApplied };
        return true;
    }

    /// <summary>
    /// Attempts reservation across ordered candidates. Failover only after a
    /// definitely-not-applied outcome; ambiguous stops everything.
    /// </summary>
    public AttemptResult ReserveAcrossCandidates(
        IReadOnlyList<string> orderedCandidateKeys,
        ProviderSearchQuery? query,
        string idempotencyKey,
        string offerKey)
    {
        var trail = new List<(string, string)>();
        foreach (var providerKey in orderedCandidateKeys)
        {
            if (IsBlockedFor(idempotencyKey))
            {
                trail.Add((providerKey, "blocked-duplicate-risk"));
                OutcomeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "duplicate-risk"));
                return new AttemptResult(AttemptState.Ambiguous, null, null, "duplicate-purchase-risk", trail);
            }

            var result = _attemptFactory(query, offerKey);

            if (result.IsSuccess)
            {
                trail.Add((providerKey, "applied"));
                OutcomeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "applied"));
                return new AttemptResult(AttemptState.Applied, providerKey, result.Value, "reserved", trail);
            }

            if (result.ErrorCode == ProviderErrorCode.AmbiguousOutcome)
            {
                // NEVER blind-retry or failover: the purchase may exist.
                _reconciliations[idempotencyKey] = new ReconciliationEntry(
                    idempotencyKey, providerKey, ReconciliationState.PendingManualReview, DateTimeOffset.UtcNow);
                trail.Add((providerKey, "ambiguous-manual-review"));
                OutcomeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "ambiguous"));
                return new AttemptResult(AttemptState.Ambiguous, providerKey, null, "ambiguous-entered-manual-review", trail);
            }

            // Definitive refusal: this attempt provably did not happen.
            trail.Add((providerKey, $"not-applied:{result.SafeErrorCode}"));
            OutcomeCounter.Add(1, new KeyValuePair<string, object?>("outcome", "not-applied"));
        }

        return new AttemptResult(AttemptState.DefinitelyNotApplied, null, null, "all-candidates-failed", trail);
    }
}
