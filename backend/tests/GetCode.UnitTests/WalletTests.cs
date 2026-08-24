using GetCode.Domain.Wallets;

namespace GetCode.UnitTests;

public sealed class MoneyTests
{
    [Fact]
    public void Currency_normalization_and_validation()
    {
        Assert.Equal("USD", Money.Zero("usd ").Currency);
        Assert.Throws<ArgumentException>(() => Money.Zero("us"));
        Assert.Throws<ArgumentException>(() => Money.Zero("USDD"));
        Assert.Throws<ArgumentException>(() => Money.Zero("U D"));
        Assert.Throws<ArgumentException>(() => Money.Zero(null));
    }

    [Fact]
    public void Major_minor_conversions_round_to_even()
    {
        Assert.Equal(1250L, Money.FromMajor(12.50m, "USD").AmountMinor);
        Assert.Equal(12.50m, Money.FromMajor(12.50m, "USD").ToMajorDecimal());
    }

    [Fact]
    public void Arithmetic_requires_same_currency()
    {
        var usd = Money.Zero("USD");
        var eur = Money.Zero("EUR");

        Assert.Equal(300L, usd.Add(new Money(300, "usd")).AmountMinor);
        _ = Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
        _ = Assert.Throws<InvalidOperationException>(() => usd.Subtract(eur));
    }

    [Fact]
    public void Negate_flips_sign_for_debits()
    {
        var amount = new Money(-500, "EUR").Validate();
        Assert.True(amount.Negate().AmountMinor > 0);
    }
}

public sealed class WalletTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Open_emits_event_and_starts_at_zero()
    {
        var wallet = Wallet.Open(Guid.CreateVersion7(), "eur", Now);

        Assert.Equal(0, wallet.BalanceMinor);
        Assert.False(wallet.IsClosed);
        Assert.Single(wallet.DomainEvents);
        Assert.IsType<WalletOpened>(wallet.DomainEvents.Single());
    }

    [Fact]
    public void Credit_then_debit_tracks_balance_with_events()
    {
        var wallet = Wallet.Open(Guid.CreateVersion7(), "USD", Now);
        wallet.ClearDomainEvents();

        Assert.Equal(10000, wallet.Credit(Money.FromMajor(100m, "USD"), LedgerEntryType.Deposit, Now));
        Assert.True(wallet.TryDebit(Money.FromMajor(40m, "USD"), Now, out var afterDebit));
        Assert.Equal(6000, afterDebit);
        Assert.Equal(6000, wallet.BalanceMinor);

        Assert.Equal(2, wallet.DomainEvents.Count); // credited + debited
    }

    [Fact]
    public void Debit_beyond_balance_is_refused_not_negative()
    {
        var wallet = Wallet.Open(Guid.CreateVersion7(), "USD", Now);
        wallet.Credit(Money.FromMajor(10m, "USD"), LedgerEntryType.Deposit, Now);

        Assert.False(wallet.TryDebit(Money.FromMajor(10.01m, "USD"), Now, out _));
        Assert.Equal(1000, wallet.BalanceMinor); // unchanged
        Assert.Equal(2, wallet.DomainEvents.Count); // refused debit emits no event
    }

    [Fact]
    public void Closed_wallet_rejects_mutations()
    {
        var wallet = Wallet.Open(Guid.CreateVersion7(), "USD", Now);
        wallet.Close(Now);

        _ = Assert.Throws<InvalidOperationException>(
            () => wallet.Credit(Money.FromMajor(5m, "USD"), LedgerEntryType.Deposit, Now));
        _ = Assert.Throws<InvalidOperationException>(() => wallet.TryDebit(Money.FromMajor(1m, "USD"), Now, out _));
    }
}

public sealed class LedgerEntryTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Append_validates_idempotency_key_and_nonzero_amount()
    {
        var walletId = Guid.CreateVersion7();
        var amount = Money.FromMajor(5m, "USD");

        _ = LedgerEntry.Append(walletId, LedgerEntryType.Deposit, amount, "key-1", 500, Now);

        Assert.Throws<ArgumentException>(() => LedgerEntry.Append(walletId, LedgerEntryType.Deposit, amount, " ", 500, Now));
        Assert.Throws<ArgumentException>(() => LedgerEntry.Append(walletId, LedgerEntryType.Deposit, amount, new string('x', 129), 500, Now));
        _ = Assert.Throws<ArgumentException>(
            () => LedgerEntry.Append(walletId, LedgerEntryType.Deposit, Money.Zero("USD"), "key-2", 500, Now));
    }

    [Fact]
    public void Deposits_must_be_credits()
    {
        var walletId = Guid.CreateVersion7();
        _ = Assert.Throws<InvalidOperationException>(
            () => LedgerEntry.Append(walletId, LedgerEntryType.Deposit, Money.FromMajor(-5m, "USD"), "k", 0, Now));
    }

    [Fact]
    public void Reference_is_normalized()
    {
        var entry = LedgerEntry.Append(
            Guid.CreateVersion7(), LedgerEntryType.Refund, Money.FromMajor(5m, "USD"), "k", 500, Now,
            referenceType: "ORDER", referenceId: Guid.CreateVersion7());
        Assert.Equal("order", entry.ReferenceType);
        Assert.True(entry.IsCredit);
    }
}
