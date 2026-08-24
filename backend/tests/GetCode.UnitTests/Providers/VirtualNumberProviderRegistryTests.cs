using GetCode.Application.Providers;

namespace GetCode.UnitTests.Providers;

/// <summary>
/// M04-007: provider registry — router/failover resolves adapters by canonical
/// key; adding a second adapter required zero business-code changes.
/// </summary>
public sealed class VirtualNumberProviderRegistryTests
{
    private sealed class StubProvider(string key) : IVirtualNumberProvider
    {
        public string ProviderKey => key;
        public Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(ProviderSearchQuery q, CancellationToken ct) =>
            Task.FromResult(ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success([]));
        public Task<ProviderResult<ProviderReservation>> ReserveAsync(ProviderReservationRequest r, CancellationToken ct) =>
            Task.FromResult(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.OfferUnavailable, "stub"));
        public Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(string id, CancellationToken ct) =>
            Task.FromResult(ProviderResult<ProviderActivationSnapshot>.Failure(ProviderErrorCode.Rejected, "stub-rejected"));
        public Task<ProviderResult> CancelAsync(string id, CancellationToken ct) => Task.FromResult(ProviderResult.Success());
    }

    [Fact]
    public void Registry_resolves_adapters_by_canonical_key_in_stable_order()
    {
        var registry = new Infrastructure.Providers.VirtualNumberProviderRegistry(
            [new StubProvider("second-vendor"), new StubProvider("fake"), new StubProvider("five-sim")]);

        Assert.Equal(["fake", "five-sim", "second-vendor"], [.. registry.Providers.Select(p => p.ProviderKey)]);
        Assert.True(registry.Contains("five-sim"));
        Assert.False(registry.Contains("unknown"));
        Assert.Equal("five-sim", registry.GetByKey("five-sim")!.ProviderKey);
        Assert.Null(registry.GetByKey("unknown"));
    }

    [Fact]
    public void Routing_policy_selects_from_registry_providers_without_name_branches()
    {
        var registry = new Infrastructure.Providers.VirtualNumberProviderRegistry(
            [new StubProvider("beta"), new StubProvider("alpha")]);

        // Candidate facts flow from the registry into the M04-005 policy — no
        // provider-name if/else anywhere.
        var decision = ProviderRoutingPolicy.Select(
            [.. registry.Providers.Select(p => new ProviderRoutingPolicy.RoutingCandidate(p.ProviderKey, PriceAmount: p.ProviderKey == "beta" ? 3m : 9m, IsAvailable: true, ConsecutiveFailures: 0))]);

        Assert.True(decision.HasSelection);
        Assert.Equal("beta", decision.SelectedProviderKey); // cheapest wins, discovered purely via registry
    }
}
