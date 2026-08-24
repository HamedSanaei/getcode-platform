using GetCode.Application.Wallets;
using GetCode.Domain.Wallets;
using GetCode.Persistence.Catalog;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Wallets;

internal sealed class WalletRepository(GetCodeDbContext context) : IWalletRepository
{
    public Task<Wallet?> FindByIdAsync(Guid walletId, CancellationToken cancellationToken) =>
        context.Wallets.FirstOrDefaultAsync(w => w.Id == walletId, cancellationToken);

    public Task<Wallet?> FindForUserAsync(Guid ownerUserId, string currency, CancellationToken cancellationToken) =>
        context.Wallets.FirstOrDefaultAsync(
            w => w.OwnerUserId == ownerUserId && w.Currency == currency,
            cancellationToken);

    public void Add(Wallet wallet) => context.Wallets.Add(wallet);
}

/// <summary>Append-only ledger adapter: exposes insert and read only.</summary>
internal sealed class LedgerRepository(GetCodeDbContext context) : ILedgerRepository
{
    public Task<LedgerEntry?> FindByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken) =>
        context.LedgerEntries.FirstOrDefaultAsync(e => e.IdempotencyKey == idempotencyKey, cancellationToken);

    public void Append(LedgerEntry entry) => context.LedgerEntries.Add(entry);

    public async Task<IReadOnlyList<LedgerEntry>> ListForWalletAsync(Guid walletId, int limit, CancellationToken cancellationToken) =>
        await context.LedgerEntries
            .Where(e => e.WalletId == walletId)
            .OrderByDescending(e => e.CreatedAtUtc)
            .ThenBy(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
}

/// <summary>
/// Translates EF concurrency/unique violations into application-level types so
/// the wallet use case can retry without referencing infrastructure.
/// </summary>
internal sealed class WalletUnitOfWork(GetCodeDbContext context) : IWalletUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new WalletConcurrencyConflictException();
        }
        catch (DbUpdateException ex) when (ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        {
            throw new IdempotencyKeyConflictException();
        }
    }

    public Task ResetAsync(CancellationToken cancellationToken)
    {
        context.ChangeTracker.Clear();
        return Task.CompletedTask;
    }
}
