using GetCode.Domain.Common;

namespace GetCode.Domain.Wallets;

public enum LedgerEntryType
{
    /// <summary>Funds in (payment captured, top-up).</summary>
    Deposit = 0,

    /// <summary>Payment for an order.</summary>
    Purchase = 1,

    /// <summary>Compensating credit after a purchase (full or partial reversal).</summary>
    Refund = 2,

    /// <summary>Manual correction by an authorized operator (wallet.adjust audit trail required).</summary>
    Adjustment = 3,
}

/// <summary>
/// Append-only ledger row. Rows are never updated or deleted; corrections are
/// new compensating entries referencing the original via ReferenceType/ReferenceId.
/// </summary>
public sealed class LedgerEntry : Entity<Guid>
{
    private LedgerEntry(
        Guid id,
        Guid walletId,
        LedgerEntryType entryType,
        long amountMinor,
        string currency,
        string? referenceType,
        Guid? referenceId,
        string idempotencyKey,
        long resultingBalanceMinor,
        DateTimeOffset createdAtUtc)
        : base(id)
    {
        WalletId = walletId;
        EntryType = entryType;
        AmountMinor = amountMinor;
        Currency = currency;
        ReferenceType = referenceType;
        ReferenceId = referenceId;
        IdempotencyKey = idempotencyKey;
        ResultingBalanceMinor = resultingBalanceMinor;
        CreatedAtUtc = createdAtUtc;
    }

    /// <summary>EF materialization constructor.</summary>
    private LedgerEntry()
        : base(Guid.Empty)
    {
        Currency = string.Empty;
        IdempotencyKey = string.Empty;
    }

    public Guid WalletId { get; }
    public LedgerEntryType EntryType { get; }
    public long AmountMinor { get; } // signed: positive credits, negative debits
    public string Currency { get; }
    public string? ReferenceType { get; }
    public Guid? ReferenceId { get; }
    public string IdempotencyKey { get; }
    public long ResultingBalanceMinor { get; }
    public DateTimeOffset CreatedAtUtc { get; }

    public bool IsCredit => AmountMinor > 0;

    public static LedgerEntry Append(
        Guid walletId,
        LedgerEntryType entryType,
        Money signedAmount,
        string idempotencyKey,
        long resultingBalanceMinor,
        DateTimeOffset nowUtc,
        string? referenceType = null,
        Guid? referenceId = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey) || idempotencyKey.Length > 128)
        {
            throw new ArgumentException("Idempotency key is required (max 128 chars).", nameof(idempotencyKey));
        }

        if (signedAmount.AmountMinor == 0)
        {
            throw new ArgumentException("Ledger entries must move a non-zero amount.", nameof(signedAmount));
        }

        if (entryType == LedgerEntryType.Deposit && signedAmount.AmountMinor < 0)
        {
            throw new InvalidOperationException("Deposits must be credits.");
        }

        var validated = signedAmount.Validate();
        return new LedgerEntry(
            id ?? Guid.CreateVersion7(),
            walletId,
            entryType,
            validated.AmountMinor,
            validated.Currency,
            referenceType is null ? null : referenceType.Trim().ToLowerInvariant(),
            referenceId,
            idempotencyKey.Trim(),
            resultingBalanceMinor,
            nowUtc);
    }
}

/// <summary>
/// A user's money account. The authoritative state is the ledger; the stored
/// balance is a projection maintained under optimistic concurrency and verified
/// by invariant checks. Balance can never go negative.
/// </summary>
public sealed class Wallet : AggregateRoot<Guid>
{
    private Wallet(Guid id, Guid ownerUserId, string currency, DateTimeOffset openedAtUtc)
        : base(id)
    {
        OwnerUserId = ownerUserId;
        Currency = currency;
        OpenedAtUtc = openedAtUtc;
    }

    /// <summary>EF materialization constructor.</summary>
    private Wallet()
        : base(Guid.Empty)
    {
        OwnerUserId = Guid.Empty;
        Currency = string.Empty;
    }

    public Guid OwnerUserId { get; }
    public string Currency { get; }
    public DateTimeOffset OpenedAtUtc { get; }

    /// <summary>Current projected balance in minor units (never negative).</summary>
    public long BalanceMinor { get; private set; }

    /// <summary>Npgsql xmin concurrency token: stale writers lose and retry.</summary>
    public uint Version { get; private set; }

    public bool IsClosed { get; private set; }

    public static Wallet Open(Guid ownerUserId, string? currency, DateTimeOffset nowUtc, Guid? id = null)
    {
        var wallet = new Wallet(id ?? Guid.CreateVersion7(), ownerUserId, Money.NormalizeCurrency(currency), nowUtc);
        wallet.Raise(new WalletOpened(wallet.Id, ownerUserId, wallet.Currency, nowUtc));
        return wallet;
    }

    /// <summary>Applies a credit; returns the new balance.</summary>
    public long Credit(Money amount, LedgerEntryType entryType, DateTimeOffset nowUtc)
    {
        EnsureOpen();
        if (amount.AmountMinor <= 0)
        {
            throw new InvalidOperationException("Credits require a positive amount.");
        }

        BalanceMinor = checked(BalanceMinor + amount.AmountMinor);
        Raise(entryType switch
        {
            LedgerEntryType.Deposit => new WalletCredited(Id, amount.AmountMinor, BalanceMinor, nowUtc),
            _ => new WalletAdjusted(Id, amount.AmountMinor, BalanceMinor, nowUtc),
        });
        return BalanceMinor;
    }

    /// <summary>Applies a debit; returns false instead of going negative.</summary>
    public bool TryDebit(Money amount, DateTimeOffset nowUtc, out long newBalance)
    {
        EnsureOpen();
        newBalance = BalanceMinor;
        if (amount.AmountMinor <= 0)
        {
            throw new InvalidOperationException("Debits require a positive amount.");
        }

        if (BalanceMinor < amount.AmountMinor)
        {
            return false; // deny-by-default: overspend is impossible by construction
        }

        BalanceMinor = checked(BalanceMinor - amount.AmountMinor);
        newBalance = BalanceMinor;
        Raise(new WalletDebited(Id, amount.AmountMinor, BalanceMinor, nowUtc));
        return true;
    }

    public void Close(DateTimeOffset nowUtc)
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;
        Raise(new WalletClosed(Id, BalanceMinor, nowUtc));
    }

    private void EnsureOpen()
    {
        if (IsClosed)
        {
            throw new InvalidOperationException("Wallet is closed to mutations.");
        }
    }
}
