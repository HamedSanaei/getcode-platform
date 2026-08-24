using GetCode.Application.Providers;

namespace GetCode.UnitTests.Providers;

/// <summary>
/// M04-004: offer normalization, cache freshness/staleness, stale-while-error
/// and store-loss degradation (Redis loss degrades to the provider path).
/// </summary>
public sealed class OfferAvailabilityCacheTests
{
    private static readonly ProviderSearchQuery Query = new("DE", "telegram", "activation");

    private static ProviderOffer Offer(string key = "germany|telegram|any", decimal cost = 7.5m) =>
        new(key, cost, "RUB", IsAvailable: true, ObservedAtUtc: DateTimeOffset.UtcNow);

    private sealed class FakeProvider : IVirtualNumberProvider
    {
        public string ProviderKey => "fake";
        public int SearchCalls { get; private set; }
        public ProviderResult<IReadOnlyCollection<ProviderOffer>> NextSearch { get; set; } =
            ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success([Offer()]);

        public Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(ProviderSearchQuery query, CancellationToken cancellationToken)
        {
            SearchCalls++;
            return Task.FromResult(NextSearch);
        }

        public Task<ProviderResult<ProviderReservation>> ReserveAsync(ProviderReservationRequest request, CancellationToken cancellationToken) =>
            throw new NotSupportedException("cache tests never purchase");

        public Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(string id, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderResult> CancelAsync(string id, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class ThrowingStore : IAvailabilityCacheStore
    {
        public CacheLookupResult<NormalizedOfferSet> Get(string cacheKey, DateTimeOffset now) => throw new IOException("store down");
        public void Put(string cacheKey, NormalizedOfferSet set) => throw new IOException("store down");
    }

    // ---- normalization ----------------------------------------------------------

    [Fact]
    public void Normalize_rejects_invalid_offers_and_stamps_observation_time()
    {
        var now = DateTimeOffset.UtcNow;
        var raw = new[]
        {
            Offer("ok|key", 5m),
            Offer("", 5m),                       // blank key rejected
            Offer("negative|key", -1m),          // negative cost rejected
            Offer("blank-currency", 3m) with { CostCurrency = "" }, // currency defaulted
        };

        var set = ProviderOfferQueryService.Normalize(Query, raw, observedAt: now, expiresAt: now.AddSeconds(30));

        Assert.Equal(2, set.Offers.Count);
        Assert.All(set.Offers, o => Assert.Equal(now, o.ObservedAtUtc));
        Assert.Contains(set.Offers, o => o.CostCurrency == "XXX");
    }

    // ---- freshness / staleness --------------------------------------------------

    [Fact]
    public async Task Fresh_cache_hit_serves_without_touching_the_provider()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var provider = new FakeProvider();
        var service = new ProviderOfferQueryService(provider, new InMemoryAvailabilityCacheStore(), clock)
        {
            FreshnessWindow = TimeSpan.FromSeconds(30),
        };

        await service.SearchAsync(Query, TestContext.Current.CancellationToken);
        var second = await service.SearchAsync(Query, TestContext.Current.CancellationToken);

        Assert.Equal(1, provider.SearchCalls); // second served from cache
        Assert.True(second.IsSuccess);
    }

    [Fact]
    public async Task Expired_entry_is_refreshed_and_stale_copy_serves_when_provider_fails()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var provider = new FakeProvider();
        var store = new InMemoryAvailabilityCacheStore();
        var service = new ProviderOfferQueryService(provider, store, clock) { FreshnessWindow = TimeSpan.FromSeconds(30) };

        // Populate a fresh copy, flip the provider to failing, then age the entry.
        await service.SearchAsync(Query, TestContext.Current.CancellationToken);
        provider.NextSearch = ProviderResult<IReadOnlyCollection<ProviderOffer>>.Failure(ProviderErrorCode.Unavailable, "transient-http");
        clock.Advance(TimeSpan.FromMinutes(5));

        // Provider now fails → explicitly-stale copy serves instead of erroring.
        var degraded = await service.SearchAsync(Query, TestContext.Current.CancellationToken);
        Assert.True(degraded.IsSuccess);
        Assert.NotEmpty(degraded.Value!);
        Assert.Equal(2, provider.SearchCalls); // populate + one refresh attempt
    }

    [Fact]
    public async Task Purchase_path_never_reads_the_cache()
    {
        // Structural pin of the AC: the query service exposes no reservation API;
        // ReserveAsync on the port takes the provider directly. The service type
        // must not expose any reserve/purchase member.
        Assert.DoesNotContain(typeof(ProviderOfferQueryService).GetMembers(),
            m => m.Name.Contains("Reserve", StringComparison.OrdinalIgnoreCase) || m.Name.Contains("Purchase", StringComparison.OrdinalIgnoreCase));
        await Task.CompletedTask;
    }

    // ---- degradation ------------------------------------------------------------

    [Fact]
    public async Task Store_loss_degrades_to_the_live_provider_path()
    {
        var provider = new FakeProvider();
        var service = new ProviderOfferQueryService(provider, new ThrowingStore());

        var result = await service.SearchAsync(Query, TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);           // truth came from the provider
        Assert.NotEmpty(result.Value!);
        Assert.Equal(1, provider.SearchCalls);
    }

    [Fact]
    public async Task Invalidate_clears_the_cached_set_so_next_read_is_a_miss()
    {
        var clock = new FixedClock(DateTimeOffset.UtcNow);
        var provider = new FakeProvider();
        var service = new ProviderOfferQueryService(provider, new InMemoryAvailabilityCacheStore(), clock);

        await service.SearchAsync(Query, TestContext.Current.CancellationToken);
        service.Invalidate(Query);
        await service.SearchAsync(Query, TestContext.Current.CancellationToken);

        Assert.Equal(2, provider.SearchCalls); // invalidation forced a live re-read
    }

    private sealed class FixedClock(DateTimeOffset initial) : TimeProvider
    {
        private DateTimeOffset _now = initial;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
