using System.Globalization;

namespace GetCode.Domain.Wallets;

/// <summary>
/// Ledger-grade money: integral minor units plus ISO-4217-style currency code.
/// All arithmetic happens in minor units to avoid floating-point drift;
/// amounts are validated to fit the signed 64-bit minor-unit storage.
/// </summary>
public readonly record struct Money(long AmountMinor, string Currency)
{
    /// <summary>Hard cap so sums cannot overflow int64 in realistic ledgers.</summary>
    public const long MaxAbsAmountMinor = 100_000_000_000_000_000L; // 1e17 minor units

    public static Money Zero(string? currency) => new(0, NormalizeCurrency(currency));

    public static Money FromMajor(decimal majorAmount, string currency)
    {
        var minor = decimal.Round(majorAmount * 100m, 0, MidpointRounding.ToEven);
        return new Money((long)minor, currency);
    }

    public decimal ToMajorDecimal() => AmountMinor / 100m;

    public static string NormalizeCurrency(string? currency)
    {
        var normalized = currency?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length != 3 || !normalized.All(char.IsAsciiLetter))
        {
            throw new ArgumentException("Currency must be a 3-letter ISO-4217 style code.", nameof(currency));
        }

        return normalized;
    }

    public Money Validate()
    {
        var c = NormalizeCurrency(Currency);
        if (Math.Abs(AmountMinor) > MaxAbsAmountMinor)
        {
            throw new ArgumentOutOfRangeException(nameof(AmountMinor), "Amount exceeds ledger storage bounds.");
        }

        return new Money(AmountMinor, c);
    }

    public Money Add(Money other) => Combine(other).With(AmountMinor + other.AmountMinor);

    public Money Subtract(Money other) => Combine(other).With(AmountMinor - other.AmountMinor);

    public Money Negate() => this.With(-AmountMinor);

    public bool IsSameCurrency(Money other) =>
        string.Equals(NormalizeCurrency(Currency), NormalizeCurrency(other.Currency), StringComparison.Ordinal);

    private Money Combine(Money other)
    {
        if (!IsSameCurrency(other))
        {
            throw new InvalidOperationException($"Currency mismatch: {Currency} vs {other.Currency}.");
        }

        return this;
    }

    private Money With(long amount) => new(amount, NormalizeCurrency(Currency));

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{ToMajorDecimal()} {Currency}");
}
