using GetCode.Application.Pricing;

namespace GetCode.UnitTests.Pricing;

/// <summary>
/// M05-001: pricing boundary tests — margin math, cent rounding (never
/// undercharge), floors, rule-version stamping and historical immutability.
/// </summary>
public sealed class PricingEngineTests
{
    private static readonly PricingRule RubRule = new(Version: 3, Currency: "RUB", MarginPercent: 25m, FixedFeeAmount: 2.00m, MinSellAmount: 10m);
    private static readonly PricingEngine Engine = new(new FixedClock(DateTimeOffset.UnixEpoch));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void Sell_price_is_cost_plus_margin_plus_fixed_fee()
    {
        var price = Engine.Compute([RubRule], "RUB", costAmount: 100m);

        Assert.Equal(127.00m, price.SellAmount); // 100 × 1.25 + 2
        Assert.Equal("RUB", price.Currency);
        Assert.Equal(100m, price.CostAmount); // input preserved for audit
        Assert.Equal(3, price.RuleVersion);
    }

    [Theory]
    [InlineData(0.001, 10)]      // tiny cost → ceiling to 1 cent → +fee = 2.01? no: 0.001*1.25+2 = 2.0013 → 2.01
    [InlineData(33.333, 43.67)]  // 33.333×1.25+2 = 43.66625 → 43.67 (rounds UP)
    [InlineData(12.004, 17.01)]  // exactly at half-cent (17.005) → rounds up
    public void Fractional_cents_round_away_from_zero_never_down(decimal cost, decimal expected)
    {
        var price = Engine.Compute([RubRule], "RUB", cost);
        // numeric comparison (scale-insensitive)
        Assert.True(price.SellAmount == expected, $"expected {expected}, got {price.SellAmount}");
    }

    [Fact]
    public void Min_sell_amount_floors_below_threshold_prices()
    {
        var price = Engine.Compute([RubRule], "RUB", costAmount: 0.0001m);
        Assert.Equal(10m, price.SellAmount); // floor applies after rounding
    }

    [Fact]
    public void Zero_margin_zero_fee_preserves_exact_cent_costs()
    {
        var rule = new PricingRule(1, "USD", MarginPercent: 0m, FixedFeeAmount: 0m, MinSellAmount: 0m);
        var price = Engine.Compute([rule], "USD", 19.99m);
        Assert.Equal(19.99m, price.SellAmount);
    }

    [Fact]
    public void Negative_cost_and_unknown_currency_fail_fast()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Engine.Compute([RubRule], "RUB", -1m));
        var ex = Assert.Throws<InvalidOperationException>(() => Engine.Compute([RubRule], "EUR", 5m));
        Assert.Contains("EUR", ex.Message); // explicit, not a silent default
    }

    [Fact]
    public void Historical_computations_are_immutable_when_rules_change()
    {
        var snapshot = Engine.Compute([RubRule], "RUB", 40m);

        var stricterRules = new List<PricingRule> { RubRule with { Version = 4, MarginPercent = 50m } };
        var recomputed = Engine.Compute(stricterRules, "RUB", 40m);

        Assert.Equal(52.00m, snapshot.SellAmount);   // old snapshot unchanged
        Assert.Equal(3, snapshot.RuleVersion);
        Assert.Equal(62.00m, recomputed.SellAmount); // new computation uses new rules
        Assert.Equal(4, recomputed.RuleVersion);
    }

    [Fact]
    public void Determinism_property_repeated_computation_is_byte_identical()
    {
        var costs = new[] { 0m, 0.01m, 7.505m, 1234.5678m, 99999999.99m };
        foreach (var cost in costs)
        {
            var first = Engine.Compute([RubRule], "RUB", cost);
            var second = Engine.Compute([RubRule], "RUB", cost);
            Assert.Equal(first.SellAmount, second.SellAmount);
            Assert.True(first.SellAmount >= cost, "sell never below provider cost");
            Assert.Equal(first.SellAmount, decimal.Round(first.SellAmount, 2)); // at most 2 decimals
        }
    }
}
