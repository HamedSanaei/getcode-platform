using GetCode.Application.Providers;
using GetCode.Infrastructure.Providers.Fake;

namespace GetCode.ProviderContractTests;

/// <summary>
/// Runs the shared behavioral contract suite against the deterministic fake
/// adapter. Real provider adapters added in M04-002+ subclass the same suite.
/// </summary>
public sealed class FakeVirtualNumberProviderContractTests : VirtualNumberProviderContractTests
{
    protected override Task<IVirtualNumberProvider> CreateProviderAsync() =>
        Task.FromResult<IVirtualNumberProvider>(new FakeVirtualNumberProvider());
}

/// <summary>Fake-specific determinism and failure-injection behavior.</summary>
public sealed class FakeProviderConfigurationTests
{
    [Fact]
    public async Task Queued_outcomes_are_consumed_in_order_then_defaults_apply()
    {
        var fake = new FakeVirtualNumberProvider();

        fake.QueueSearchOutcome(ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success([]));
        var first = await fake.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), TestContext.Current.CancellationToken);
        Assert.Empty(first.Value!);

        // Queue drained: deterministic default applies again.
        var second = await fake.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), TestContext.Current.CancellationToken);
        Assert.NotEmpty(second.Value!);
    }

    [Fact]
    public async Task Injected_failures_surface_with_configured_codes()
    {
        var fake = new FakeVirtualNumberProvider();
        fake.QueueReserveOutcome(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.RateLimited, "rate_limited"));

        var result = await fake.ReserveAsync(new ProviderReservationRequest("offer-1", $"inj-{Guid.NewGuid():N}", "corr"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProviderErrorCode.RateLimited, result.ErrorCode);
        Assert.Equal("rate_limited", result.SafeErrorCode);
    }

    [Fact]
    public async Task Latency_is_simulated_and_cancellable()
    {
        var fake = new FakeVirtualNumberProvider { Latency = TimeSpan.FromSeconds(5) };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            fake.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), cts.Token));
    }

    [Fact]
    public async Task Seeded_reservations_enable_status_and_cancel_flows()
    {
        var fake = new FakeVirtualNumberProvider();
        var seeded = fake.SeedReservation("fake-seeded-1");

        var snapshot = await fake.GetActivationAsync(seeded.ProviderOperationId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderActivationState.WaitingForMessage, snapshot.Value!.State);

        await fake.CancelAsync(seeded.ProviderOperationId, TestContext.Current.CancellationToken);
        var cancelled = await fake.GetActivationAsync(seeded.ProviderOperationId, TestContext.Current.CancellationToken);
        Assert.Equal(ProviderActivationState.Cancelled, cancelled.Value!.State);
    }
}
