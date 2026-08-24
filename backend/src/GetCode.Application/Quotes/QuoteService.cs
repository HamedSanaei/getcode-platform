using System.Collections.Concurrent;
using GetCode.Application.Pricing;

namespace GetCode.Application.Quotes;

/// <summary>
/// M05-002: immutable, expiring quote snapshots. A quote binds product identity,
/// the authoritative customer-visible price/currency and an expiry — issued once,
/// never mutated. Checkout revalidates against the STORED quote: expired quotes
/// are rejected (410-semantics), amounts that disagree with the stored snapshot
/// are treated as tampering, and refreshing always issues a NEW quote so history
/// stays intact.
/// <para>
/// The provider-cost trace needed for operations lives in a separate record and
/// is never part of the customer view. Store is in-memory for this task; durable
/// persistence joins the M06-002 checkout transaction.
/// </para>
/// </summary>
public sealed record ProviderCostTrace(string ProviderKey, decimal CostAmount, string CostCurrency, int PricingRuleVersion);

public sealed record QuoteSnapshot(
    Guid QuoteId,
    string CountryKey,
    string ServiceKey,
    string ProductTypeKey,
    decimal CustomerAmount,
    string Currency,
    DateTimeOffset IssuedAtUtc,
    DateTimeOffset ExpiresAtUtc,
    int PricingRuleVersion)
{
    public bool IsExpired(DateTimeOffset now) => now >= ExpiresAtUtc;
}

public sealed record IssuedQuote(QuoteSnapshot CustomerView, ProviderCostTrace CostTrace);

public sealed record IssueQuoteRequest(string CountryKey, string ServiceKey, string ProductTypeKey, string ProviderKey, decimal ProviderCostAmount, string CostCurrency);

public enum QuoteValidation { Valid = 0, NotFound = 1, Expired = 2, Tampered = 3 }

public sealed class QuoteOptions
{
    public const string SectionName = "Quotes";
    public int TtlSeconds { get; set; } = 300;
}

public sealed class QuoteService(PricingEngine pricing, QuoteOptions? options = null, TimeProvider? clock = null)
{
    private readonly ConcurrentDictionary<Guid, (QuoteSnapshot Snapshot, ProviderCostTrace Trace)> _quotes = new();
    private readonly TimeSpan _ttl = TimeSpan.FromSeconds(Math.Clamp(options?.TtlSeconds ?? 300, 5, 86400));
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    public IssuedQuote Issue(IssueQuoteRequest request)
    {
        var rule = DefaultRuleSet.For(request.CostCurrency);
        var price = pricing.Compute(rule, request.CostCurrency, request.ProviderCostAmount);
        var now = _clock.GetUtcNow();
        var snapshot = new QuoteSnapshot(
            Guid.NewGuid(), request.CountryKey, request.ServiceKey, request.ProductTypeKey,
            price.SellAmount, price.Currency, now, now.Add(_ttl), price.RuleVersion);
        var trace = new ProviderCostTrace(request.ProviderKey, request.ProviderCostAmount, request.CostCurrency, price.RuleVersion);
        _quotes[snapshot.QuoteId] = (snapshot, trace);
        return new IssuedQuote(snapshot, trace);
    }

    /// <summary>Customer-facing validation: never exposes the provider cost.</summary>
    public (QuoteValidation Result, QuoteSnapshot? Snapshot) ValidateForCheckout(Guid quoteId, decimal presentedAmount)
    {
        if (!_quotes.TryGetValue(quoteId, out var entry))
        {
            return (QuoteValidation.NotFound, null);
        }

        if (entry.Snapshot.IsExpired(_clock.GetUtcNow()))
        {
            return (QuoteValidation.Expired, null);
        }

        // Amount must match the stored authoritative snapshot exactly.
        return entry.Snapshot.CustomerAmount == presentedAmount
            ? (QuoteValidation.Valid, entry.Snapshot)
            : (QuoteValidation.Tampered, null);
    }

    /// <summary>Refresh issues a brand-new quote at CURRENT rules; the old one is left untouched (immutable history).</summary>
    public IssuedQuote Refresh(Guid expiredQuoteId)
    {
        if (!_quotes.TryGetValue(expiredQuoteId, out var entry))
        {
            throw new KeyNotFoundException("quote-not-found");
        }

        var old = entry.Snapshot;
        return Issue(new IssueQuoteRequest(old.CountryKey, old.ServiceKey, old.ProductTypeKey, entry.Trace.ProviderKey, entry.Trace.CostAmount, entry.Trace.CostCurrency));
    }
}

/// <summary>M05-001 wiring point: per-currency rules until config binding lands with checkout.</summary>
public static class DefaultRuleSet
{
    public static IReadOnlyList<PricingRule> For(string currency) => currency switch
    {
        "RUB" => [new PricingRule(1, "RUB", MarginPercent: 25m, FixedFeeAmount: 2m, MinSellAmount: 10m)],
        "USD" => [new PricingRule(1, "USD", MarginPercent: 20m, FixedFeeAmount: 0.30m, MinSellAmount: 0.50m)],
        _ => [new PricingRule(1, currency, MarginPercent: 25m, FixedFeeAmount: 0m, MinSellAmount: 1m)],
    };
}
