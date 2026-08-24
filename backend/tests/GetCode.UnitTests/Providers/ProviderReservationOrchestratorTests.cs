using GetCode.Application.Providers;

namespace GetCode.UnitTests.Providers;

/// <summary>
/// M04-006: safe failover + ambiguous-outcome reconciliation — timeout-after-
/// send never triggers blind retry/failover, duplicate reservations are
/// prevented, and ambiguous cases reconcile or enter Manual Review.
/// </summary>
public sealed class ProviderReservationOrchestratorTests
{
    private readonly List<(string? Query, string? OfferKey)> _attempts = [];
    private readonly Queue<ProviderResult<ProviderReservation>> _outcomes = new();

    private ProviderReservationOrchestrator Create() =>
        new((query, offerKey) =>
        {
            _attempts.Add((query?.CountryKey, offerKey));
            return _outcomes.Dequeue();
        });

    private static ProviderResult<ProviderReservation> Applied(string opId = "op-1") =>
        ProviderResult<ProviderReservation>.Success(new ProviderReservation(opId, "+79001234567", DateTimeOffset.UtcNow, null));

    [Fact]
    public void Definitive_failure_fails_over_to_the_next_candidate()
    {
        var orchestrator = Create();
        _outcomes.Enqueue(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.OfferUnavailable, "no-inventory"));
        _outcomes.Enqueue(Applied("op-2"));

        var result = orchestrator.ReserveAcrossCandidates(["alpha", "beta"], null, "idem-1", "offer");

        Assert.Equal(ProviderReservationOrchestrator.AttemptState.Applied, result.State);
        Assert.Equal("beta", result.SelectedProviderKey);
        Assert.Equal(2, _attempts.Count); // both providers attempted in order
    }

    [Fact]
    public async Task Timeout_after_send_is_ambiguous_and_forbidden_from_failover()
    {
        var orchestrator = Create();
        _outcomes.Enqueue(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase"));

        var result = await Task.Run(() => orchestrator.ReserveAcrossCandidates(
            ["alpha", "beta", "gamma"], null, "idem-timeout", "offer"), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderReservationOrchestrator.AttemptState.Ambiguous, result.State);
        Assert.Single(_attempts); // NO failover after ambiguity
        Assert.Equal("ambiguous-entered-manual-review", result.ReasonToken);
        Assert.True(orchestrator.IsBlockedFor("idem-timeout"));
        Assert.Contains(orchestrator.PendingReconciliations, e => e.IdempotencyKey == "idem-timeout" && e.State == ProviderReservationOrchestrator.ReconciliationState.PendingManualReview);
    }

    [Fact]
    public async Task Duplicate_reservation_after_ambiguity_is_refused_even_with_new_candidates()
    {
        var orchestrator = Create();
        _outcomes.Enqueue(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase"));
        await Task.Run(() => orchestrator.ReserveAcrossCandidates(["alpha"], null, "idem-dup", "offer"), TestContext.Current.CancellationToken);

        // A later attempt with the SAME idempotency key must be refused outright.
        var before = _attempts.Count;
        var refused = orchestrator.ReserveAcrossCandidates(["beta", "gamma"], null, "idem-dup", "offer2");

        Assert.Equal(ProviderReservationOrchestrator.AttemptState.Ambiguous, refused.State);
        Assert.Equal("duplicate-purchase-risk", refused.ReasonToken);
        Assert.Equal(before, _attempts.Count); // no provider was contacted at all
    }

    [Fact]
    public async Task Reconciliation_resolves_not_applied_and_unblocks_the_key()
    {
        var orchestrator = Create();
        _outcomes.Enqueue(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase"));
        await Task.Run(() => orchestrator.ReserveAcrossCandidates(["alpha"], null, "idem-rec", "offer"), TestContext.Current.CancellationToken);

        Assert.True(orchestrator.ResolveNotApplied("idem-rec"));
        Assert.False(orchestrator.IsBlockedFor("idem-rec")); // evidence: purchase did not happen

        // After resolution a fresh attempt may proceed (and now succeeds).
        _outcomes.Enqueue(Applied("op-9"));
        var retried = orchestrator.ReserveAcrossCandidates(["beta"], null, "idem-rec", "offer");
        Assert.Equal(ProviderReservationOrchestrator.AttemptState.Applied, retried.State);
    }

    [Fact]
    public async Task Reconciling_an_unknown_or_already_resolved_key_fails_cleanly()
    {
        var orchestrator = Create();
        Assert.False(orchestrator.ResolveNotApplied("never-seen"));

        _outcomes.Enqueue(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase"));
        await Task.Run(() => orchestrator.ReserveAcrossCandidates(["alpha"], null, "idem-twice", "offer"), TestContext.Current.CancellationToken);
        Assert.True(orchestrator.ResolveNotApplied("idem-twice"));
        Assert.False(orchestrator.ResolveNotApplied("idem-twice")); // already resolved
    }
}
