using System.Diagnostics.Metrics;

namespace GetCode.Application.Providers;

/// <summary>
/// M04-005: provider routing policy v1 — an isolated, deterministic policy.
/// Inputs are plain candidate facts (price, availability, health streak);
/// no provider names branch anywhere in business code. The decision carries a
/// stable safe reason token for telemetry and audit.
/// <para>
/// Rules (v1): exclude unavailable candidates and those at/below the
/// unreachable failure threshold; among survivors pick the lowest price;
/// ties break deterministically by provider key (ordinal). Health data is
/// supplier telemetry from M04-003 — never wallet state.
/// </para>
/// </summary>
public static class ProviderRoutingPolicy
{
    public const string MeterName = "GetCode.ProviderRouting";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> DecisionCounter =
        Meter.CreateCounter<long>("provider.routing.decisions", description: "Routing decisions by reason");

    public const int UnreachableFailureThreshold = 3;

    public sealed record RoutingCandidate(
        string ProviderKey,
        decimal PriceAmount,
        bool IsAvailable,
        int ConsecutiveFailures);

    public sealed record RoutingDecision(
        string? SelectedProviderKey,
        string ReasonToken,
        IReadOnlyList<(string ProviderKey, bool Considered, string Detail)> Evaluations)
    {
        public bool HasSelection => SelectedProviderKey is not null;
    }

    public static RoutingDecision Select(IReadOnlyList<RoutingCandidate> candidates)
    {
        var decision = SelectCore(candidates);
        DecisionCounter.Add(1, new KeyValuePair<string, object?>("reason", decision.ReasonToken));
        return decision;
    }

    private static RoutingDecision SelectCore(IReadOnlyList<RoutingCandidate> candidates)
    {
        if (candidates.Count == 0)
        {
            return new RoutingDecision(null, "no-candidates", []);
        }

        var evaluations = new List<(string, bool, string)>(candidates.Count);
        foreach (var c in candidates.OrderBy(c => c.ProviderKey, StringComparer.Ordinal))
        {
            if (!c.IsAvailable)
            {
                evaluations.Add((c.ProviderKey, false, "unavailable"));
            }
            else if (c.ConsecutiveFailures >= UnreachableFailureThreshold)
            {
                evaluations.Add((c.ProviderKey, false, "unhealthy"));
            }
            else
            {
                evaluations.Add((c.ProviderKey, true, $"considered price={c.PriceAmount}"));
            }
        }

        var viable = candidates
            .Where(c => c.IsAvailable && c.ConsecutiveFailures < UnreachableFailureThreshold)
            .OrderBy(c => c.PriceAmount)
            .ThenBy(c => c.ProviderKey, StringComparer.Ordinal)
            .ToList();

        if (viable.Count == 0)
        {
            return new RoutingDecision(null, "all-unavailable-or-unhealthy", evaluations);
        }

        var winner = viable[0];
        // Tie reason when the same lowest price occurs more than once.
        var tied = viable.Count(c => c.PriceAmount == winner.PriceAmount) > 1;
        var reason = tied ? "selected-tie-broken-by-key" : viable.Count == 1 ? "only-viable-candidate" : "selected-lowest-price";
        return new RoutingDecision(winner.ProviderKey, reason, evaluations);
    }
}
