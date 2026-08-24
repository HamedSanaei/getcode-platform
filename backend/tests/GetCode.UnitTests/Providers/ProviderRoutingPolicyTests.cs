using GetCode.Application.Providers;

namespace GetCode.UnitTests.Providers;

/// <summary>
/// M04-005: routing policy v1 — deterministic selection, health/price inputs,
/// tie-breaking, failure reasons. No provider-name branching exists by design.
/// </summary>
public sealed class ProviderRoutingPolicyTests
{
    private static ProviderRoutingPolicy.RoutingCandidate Candidate(
        string key, decimal price = 5m, bool available = true, int failures = 0) =>
        new(key, price, available, failures);

    // ---- routing unit tests -----------------------------------------------------

    [Fact]
    public void Lowest_price_viable_candidate_wins()
    {
        var decision = ProviderRoutingPolicy.Select(
        [
            Candidate("alpha", 7m),
            Candidate("beta", 5m),
            Candidate("gamma", 9m),
        ]);

        Assert.True(decision.HasSelection);
        Assert.Equal("beta", decision.SelectedProviderKey);
        Assert.Equal("selected-lowest-price", decision.ReasonToken);
    }

    [Fact]
    public void Unavailable_and_unhealthy_candidates_are_excluded_from_consideration()
    {
        var decision = ProviderRoutingPolicy.Select(
        [
            Candidate("cheap-but-down", 1m, available: false),
            Candidate("failing-provider", 2m, failures: ProviderRoutingPolicy.UnreachableFailureThreshold),
            Candidate("healthy", 6m),
        ]);

        Assert.Equal("healthy", decision.SelectedProviderKey);
        var evals = decision.Evaluations.ToDictionary(e => e.Item1);
        Assert.False(evals["cheap-but-down"].Item2);
        Assert.Equal("unavailable", evals["cheap-but-down"].Item3);
        Assert.False(evals["failing-provider"].Item2);
        Assert.Equal("unhealthy", evals["failing-provider"].Item3);
    }

    [Fact]
    public void Degraded_provider_below_threshold_still_routes()
    {
        var decision = ProviderRoutingPolicy.Select([Candidate("degraded", 4m, failures: 2)]);

        Assert.True(decision.HasSelection);
        Assert.Equal("degraded", decision.SelectedProviderKey);
    }

    // ---- tie tests ---------------------------------------------------------------

    [Fact]
    public void Price_ties_break_deterministically_by_provider_key()
    {
        var a = ProviderRoutingPolicy.Select([Candidate("zeta", 5m), Candidate("alpha", 5m), Candidate("mid", 5m)]);
        var b = ProviderRoutingPolicy.Select([Candidate("mid", 5m), Candidate("zeta", 5m), Candidate("alpha", 5m)]);

        Assert.Equal("alpha", a.SelectedProviderKey); // ordinal order, not input order
        Assert.Equal("selected-tie-broken-by-key", a.ReasonToken);
        Assert.Equal(a.SelectedProviderKey, b.SelectedProviderKey); // input order irrelevant
    }

    // ---- failure tests -------------------------------------------------------------

    [Fact]
    public void Empty_candidate_list_yields_no_candidates_reason()
    {
        var decision = ProviderRoutingPolicy.Select([]);
        Assert.False(decision.HasSelection);
        Assert.Equal("no-candidates", decision.ReasonToken);
    }

    [Fact]
    public void All_candidates_unavailable_or_unhealthy_fails_with_explicit_reason()
    {
        var decision = ProviderRoutingPolicy.Select(
        [
            Candidate("down", 1m, available: false),
            Candidate("sick", 2m, failures: 10),
        ]);

        Assert.False(decision.HasSelection);
        Assert.Equal("all-unavailable-or-unhealthy", decision.ReasonToken);
    }

    [Fact]
    public void Single_viable_candidate_reports_its_own_reason()
    {
        var decision = ProviderRoutingPolicy.Select([Candidate("only", 3m)]);
        Assert.Equal("only-viable-candidate", decision.ReasonToken);
        Assert.Equal("only", decision.SelectedProviderKey);
    }
}
