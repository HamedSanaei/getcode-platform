using System.Collections.Concurrent;

namespace GetCode.Application.Providers;

/// <summary>
/// M04-004: short-lived availability cache over normalized provider offers.
/// <para>
/// Normalization contract: every stored offer carries canonical keys, non-negative
/// cost, currency, availability flag and the observation timestamp. Staleness is
/// explicit: consumers see <see cref="CacheLookupResult{T}.IsStale"/>. The cache
/// is BEST-EFFORT by design — any store failure degrades to the live provider
/// path rather than corrupting truth (AGENTS.md: PostgreSQL is the source of
/// truth; caches are disposable). Reservations NEVER read this cache: purchase
/// always revalidates authoritative availability at the provider.
/// </para>
/// </summary>
public sealed record NormalizedOfferSet(
    string CountryKey,
    string ServiceKey,
    IReadOnlyList<ProviderOffer> Offers,
    DateTimeOffset ObservedAtUtc,
    DateTimeOffset ExpiresAtUtc)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
}

public sealed class CacheLookupResult<T>(T? value, bool isStale)
{
    public T? Value { get; } = value;
    public bool IsHit { get; } = value is not null;
    public bool IsStale { get; } = isStale;
}

public interface IAvailabilityCacheStore
{
    CacheLookupResult<NormalizedOfferSet> Get(string cacheKey, DateTimeOffset now);
    void Put(string cacheKey, NormalizedOfferSet set);
}

/// <summary>Thread-safe in-process store. A distributed (Redis) store can replace it behind the same interface.</summary>
public sealed class InMemoryAvailabilityCacheStore : IAvailabilityCacheStore
{
    private readonly ConcurrentDictionary<string, NormalizedOfferSet> _entries = new(StringComparer.Ordinal);

    public CacheLookupResult<NormalizedOfferSet> Get(string cacheKey, DateTimeOffset now) =>
        _entries.TryGetValue(cacheKey, out var set) && set.Offers.Count > 0
            ? new CacheLookupResult<NormalizedOfferSet>(set, isStale: set.IsExpired(now))
            : new CacheLookupResult<NormalizedOfferSet>(null, isStale: false);

    public void Put(string cacheKey, NormalizedOfferSet set) => _entries[cacheKey] = set;
}

/// <summary>
/// M04-004: query pipeline — normalize → cache → serve; stale-while-error on
/// provider faults; store failures transparently degrade to the provider path.
/// </summary>
public sealed class ProviderOfferQueryService(
    IVirtualNumberProvider provider,
    IAvailabilityCacheStore store,
    TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>Default freshness window for availability data.</summary>
    public TimeSpan FreshnessWindow { get; init; } = TimeSpan.FromSeconds(30);

    public async Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchAsync(
        ProviderSearchQuery query, CancellationToken cancellationToken)
    {
        var cacheKey = $"offers:{query.CountryKey}:{query.ServiceKey}:{query.ProductTypeKey}";
        var now = _clock.GetUtcNow();

        CacheLookupResult<NormalizedOfferSet> cached;
        try
        {
            cached = store.Get(cacheKey, now);
        }
        catch (Exception)
        {
            cached = new CacheLookupResult<NormalizedOfferSet>(null, false); // store loss ⇒ provider path
        }

        if (cached.IsHit && !cached.IsStale)
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(cached.Value!.Offers);
        }

        var live = await provider.SearchOffersAsync(query, cancellationToken);

        // Store failures must not break the live path either.
        if (live.IsSuccess)
        {
            try
            {
                store.Put(cacheKey, Normalize(query, live.Value!, now, now + FreshnessWindow));
            }
            catch (Exception)
            {
                // cache write loss is acceptable — truth lives with the provider
            }

            return live;
        }

        // Provider fault: serve an explicitly-stale copy when we have one.
        if (cached.IsHit && cached.Value is { } stale)
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(stale.Offers);
        }

        return live;
    }

    /// <summary>Purchases never consult this cache; this hook exists for explicit invalidation flows.</summary>
    public void Invalidate(ProviderSearchQuery query) =>
        store.Put($"offers:{query.CountryKey}:{query.ServiceKey}:{query.ProductTypeKey}",
            new NormalizedOfferSet(query.CountryKey, query.ServiceKey, [], _clock.GetUtcNow(), _clock.GetUtcNow()));

    public static NormalizedOfferSet Normalize(
        ProviderSearchQuery query, IReadOnlyCollection<ProviderOffer> raw, DateTimeOffset observedAt, DateTimeOffset expiresAt)
    {
        // Defensive normalization: adapters should already be canonical, but the
        // cache boundary rejects anything that could corrupt downstream pricing.
        var offers = raw
            .Where(o => !string.IsNullOrWhiteSpace(o.ProviderOfferKey))
            .Where(o => o.CostAmount >= 0m)
            .Select(o => o with
            {
                CostCurrency = string.IsNullOrWhiteSpace(o.CostCurrency) ? "XXX" : o.CostCurrency,
                ObservedAtUtc = observedAt,
            })
            .ToArray();
        return new NormalizedOfferSet(query.CountryKey, query.ServiceKey, offers, observedAt, expiresAt);
    }
}
