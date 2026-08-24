using GetCode.Application.Pricing;
using GetCode.Application.Quotes;

namespace GetCode.UnitTests.Pricing;

/// <summary>
/// M05-002: quote expiry/tamper semantics, safe refresh, provider-cost
/// separation from the customer view.
/// </summary>
public sealed class QuoteServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly IssueQuoteRequest Request = new("RU", "telegram", "activation", "five-sim", 100m, "RUB");

    private static QuoteService Create(DateTimeOffset? now = null, int ttlSeconds = 300) =>
        new(new PricingEngine(new FixedClock(now ?? T0)), new QuoteOptions { TtlSeconds = ttlSeconds }, new FixedClock(now ?? T0));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        private DateTimeOffset _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }

    [Fact]
    public void Issued_quote_binds_product_price_expiry_and_rule_version()
    {
        var issued = Create().Issue(Request);

        Assert.Equal("RU", issued.CustomerView.CountryKey);
        Assert.Equal(127m, issued.CustomerView.CustomerAmount); // M05-001 math
        Assert.Equal("RUB", issued.CustomerView.Currency);
        Assert.Equal(T0.AddSeconds(300), issued.CustomerView.ExpiresAtUtc);
        Assert.Equal(1, issued.CustomerView.PricingRuleVersion);
        Assert.NotEqual(Guid.Empty, issued.CustomerView.QuoteId);
    }

    [Fact]
    public void Checkout_accepts_exact_amount_within_ttl()
    {
        var service = Create();
        var issued = service.Issue(Request);

        var (result, snapshot) = service.ValidateForCheckout(issued.CustomerView.QuoteId, 127m);

        Assert.Equal(QuoteValidation.Valid, result);
        Assert.NotNull(snapshot);
    }

    [Fact]
    public void Expired_quotes_are_rejected_even_with_correct_amount()
    {
        var clock = new FixedClock(T0);
        var service = new QuoteService(new PricingEngine(clock), new QuoteOptions { TtlSeconds = 300 }, clock);
        var issued = service.Issue(Request);

        var before = service.ValidateForCheckout(issued.CustomerView.QuoteId, 127m);
        clock.Advance(TimeSpan.FromSeconds(301)); // past expiry
        var after = service.ValidateForCheckout(issued.CustomerView.QuoteId, 127m);

        Assert.Equal(QuoteValidation.Valid, before.Result);
        Assert.Equal(QuoteValidation.Expired, after.Result);
        Assert.Null(after.Snapshot);
    }

    [Fact]
    public void Tampered_amount_is_rejected_against_stored_authoritative_snapshot()
    {
        var service = Create();
        var issued = service.Issue(Request);

        var tamperedResult = service.ValidateForCheckout(issued.CustomerView.QuoteId, 126.99m);
        var unknownResult = service.ValidateForCheckout(Guid.NewGuid(), 127m);

        Assert.True(tamperedResult.Result == QuoteValidation.Tampered, $"got {tamperedResult.Result}");
        Assert.True(unknownResult.Result == QuoteValidation.NotFound, $"got {unknownResult.Result}");
    }

    [Fact]
    public void Refresh_issues_a_new_quote_at_current_rules_leaving_history_intact()
    {
        var service = Create();
        var first = service.Issue(Request);
        var refreshed = service.Refresh(first.CustomerView.QuoteId);

        Assert.NotEqual(first.CustomerView.QuoteId, refreshed.CustomerView.QuoteId);
        Assert.Equal(first.CustomerView.CustomerAmount, refreshed.CustomerView.CustomerAmount); // same rules → same price

        // Old quote remains independently valid until ITS expiry.
        var (oldResult, _) = service.ValidateForCheckout(first.CustomerView.QuoteId, 127m);
        Assert.Equal(QuoteValidation.Valid, oldResult);
    }

    [Fact]
    public void Customer_view_never_carries_provider_cost_trace()
    {
        var issued = Create().Issue(Request);

        // Structural pin: QuoteSnapshot has no cost fields; the trace is a separate type.
        Assert.DoesNotContain(typeof(QuoteSnapshot).GetProperties(), p => p.Name.Contains("Cost", StringComparison.OrdinalIgnoreCase) || p.Name.Contains("Provider", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(100m, issued.CostTrace.CostAmount); // ops data exists, but only here
    }
}
