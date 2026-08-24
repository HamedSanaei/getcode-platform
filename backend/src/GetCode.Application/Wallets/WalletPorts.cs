using GetCode.Domain.Wallets;

namespace GetCode.Application.Wallets;

public interface IWalletRepository
{
    Task<Wallet?> FindByIdAsync(Guid walletId, CancellationToken cancellationToken);
    Task<Wallet?> FindForUserAsync(Guid ownerUserId, string currency, CancellationToken cancellationToken);
    void Add(Wallet wallet);
}

/// <summary>Append-only access to ledger rows; no update or delete paths exist.</summary>
public interface ILedgerRepository
{
    /// <summary>Returns the existing entry when the idempotency key was already used (replay).</summary>
    Task<LedgerEntry?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken);
    void Append(LedgerEntry entry);
    Task<IReadOnlyList<LedgerEntry>> ListForWalletAsync(Guid walletId, int limit, CancellationToken cancellationToken);
}

/// <summary>
/// Money mutations are transactional: the wallet row update and its ledger
/// append commit or roll back together.
/// </summary>
public interface IWalletUnitOfWork
{
    /// <summary>Commits atomically. Throws <see cref="WalletConcurrencyConflictException"/> when another writer moved the wallet row first, or <see cref="IdempotencyKeyConflictException"/> when a concurrent call claimed the same key.</summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Drops all tracked/failed work so the next attempt starts from clean database state.</summary>
    Task ResetAsync(CancellationToken cancellationToken);
}

public sealed record OpenWalletCommand(Guid OwnerUserId, string Currency, string? CorrelationId = null);

public sealed record WalletMutationCommand(
    Guid OwnerUserId,
    LedgerEntryType EntryType,
    decimal MajorAmount,
    string IdempotencyKey,
    string? ReferenceType = null,
    Guid? ReferenceId = null,
    string? CorrelationId = null,
    string? Currency = null);

public sealed record MutationOutcome(
    bool Success,
    long BalanceMinorAfter,
    string Currency,
    bool ReplayedExistingEntry)
{
    public static MutationOutcome Applied(long balanceMinorAfter, string currency) => new(true, balanceMinorAfter, currency, false);
    public static MutationOutcome Replayed(long balanceMinorAfter, string currency) => new(true, balanceMinorAfter, currency, true);
    public static MutationOutcome InsufficientFunds(long balanceMinor, string currency) => new(false, balanceMinor, currency, false);
}

/// <summary>Raised when the wallet row changed underneath us (xmin mismatch).</summary>
public sealed class WalletConcurrencyConflictException : Exception
{
    public WalletConcurrencyConflictException()
        : base("Concurrent wallet mutation detected.")
    {
    }
}

/// <summary>Raised when another request committed an entry with the same idempotency key first.</summary>
public sealed class IdempotencyKeyConflictException : Exception
{
    public IdempotencyKeyConflictException()
        : base("Idempotency key was claimed concurrently.")
    {
    }
}
