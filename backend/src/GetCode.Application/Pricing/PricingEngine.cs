namespace GetCode.Application.Pricing;

/// <summary>
/// M05-001: explicit pricing rules and margin model. The authoritative sell
/// price is ALWAYS computed server-side from these explicit rules — never from
/// client input and never by mutating stored orders.
/// <para>
/// Formula (per currency rule): sell = roundUpToCent(cost × (1 + MarginPercent/100)
/// + FixedFeeAmount), floored at MinSellAmount. Rounding is AWAY FROM ZERO at the
/// 3rd decimal (ceiling semantics for us: we never undercharge). Rules carry a
/// Version; results stamp that version so historical orders/quotes remain
/// reproducible and never change when rules change later.
/// </para>
/// </summary>
public sealed record PricingRule(
    int Version,
    string Currency,
    decimal MarginPercent,
    decimal FixedFeeAmount,
    decimal MinSellAmount)
{
    public static readonly IReadOnlyList<PricingRule> Empty = [];
}

public sealed record PriceComputation(
    string Currency,
    decimal CostAmount,
    decimal SellAmount,
    int RuleVersion,
    DateTimeOffset ComputedAtUtc);

public sealed class PricingEngine(TimeProvider? clock = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <summary>Compute the authoritative sell price for one provider cost.</summary>
    public PriceComputation Compute(IReadOnlyList<PricingRule> rules, string currency, decimal costAmount)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("currency required", nameof(currency));
        }

        if (costAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(costAmount), "provider cost cannot be negative");
        }

        var rule = rules.FirstOrDefault(r => r.Currency == currency)
            ?? throw new InvalidOperationException($"no-pricing-rule-for-{currency}");

        var marked = costAmount * (1m + rule.MarginPercent / 100m) + rule.FixedFeeAmount;

        // Ceiling to cent: any fraction of a cent rounds UP (never undercharge).
        var cents = Math.Ceiling(marked * 100m);
        var sell = cents / 100m;

        if (sell < rule.MinSellAmount)
        {
            sell = rule.MinSellAmount;
        }

        return new PriceComputation(currency, costAmount, sell, rule.Version, _clock.GetUtcNow());
    }
}
