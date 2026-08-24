using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GetCode.Application.Catalog;
using GetCode.Domain.Wallets;

namespace GetCode.Application.Wallets;

/// <summary>
/// Ledger-based money mutations (AGENTS.md rule 7): every wallet mutation
/// appends exactly one immutable ledger entry inside the same transaction as
/// the wallet balance update. Idempotency semantics (M05-004):
/// <list type="bullet">
/// <item>same idempotency key + same semantic request replays the committed outcome;</item>
/// <item>same key + conflicting payload is rejected and audited (<c>wallet.idempotency_conflict</c>);</item>
/// <item>a crash between commit and response is safe: the retry replays the winner.</item>
/// </list>
/// Concurrent debits are serialized by optimistic concurrency (xmin token) with
/// bounded retry + backoff, so overspending is impossible by construction.
/// </summary>
public sealed class WalletService(
    IWalletRepository wallets,
    ILedgerRepository ledger,
    IWalletUnitOfWork unitOfWork,
    IOutboxCollector outbox)
{
    private const int MaxConcurrencyRetries = 6;
    public const string DefaultCurrency = "USD";

    public async Task<Guid> OpenWalletAsync(OpenWalletCommand command, CancellationToken cancellationToken)
    {
        var currency = Money.NormalizeCurrency(command.Currency);
        var existing = await wallets.FindForUserAsync(command.OwnerUserId, currency, cancellationToken);
        if (existing is not null)
        {
            return existing.Id; // idempotent open
        }

        var wallet = Wallet.Open(command.OwnerUserId, currency, DateTimeOffset.UtcNow);
        wallets.Add(wallet);

        Collect("wallet.opened", new { walletId = wallet.Id, ownerUserId = wallet.OwnerUserId, currency }, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return wallet.Id;
    }

    /// <summary>Deposits funds (credit). Replays safely on duplicate idempotency keys.</summary>
    public Task<MutationOutcome> DepositAsync(WalletMutationCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command with { EntryType = LedgerEntryType.Deposit }, signedCredit: true, cancellationToken);

    /// <summary>Debits funds for a purchase. Insufficient balance is a failure result, never a negative balance.</summary>
    public Task<MutationOutcome> PurchaseAsync(WalletMutationCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command with { EntryType = LedgerEntryType.Purchase }, signedCredit: false, cancellationToken);

    /// <summary>Compensating credit referencing an original purchase; never rewrites history.</summary>
    public Task<MutationOutcome> RefundAsync(WalletMutationCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command with { EntryType = LedgerEntryType.Refund }, signedCredit: true, cancellationToken);

    /// <summary>Signed operator correction; separate compensating entry with audit trail.</summary>
    public Task<MutationOutcome> AdjustAsync(WalletMutationCommand command, CancellationToken cancellationToken) =>
        ApplyAsync(command with { EntryType = LedgerEntryType.Adjustment }, signedCredit: null, cancellationToken);

    private async Task<MutationOutcome> ApplyAsync(WalletMutationCommand command, bool? signedCredit, CancellationToken cancellationToken)
    {
        var amount = Money.FromMajor(command.MajorAmount, command.Currency ?? DefaultCurrency);
        if (signedCredit == true && amount.AmountMinor <= 0 || signedCredit == false && amount.AmountMinor <= 0)
        {
            throw new InvalidOperationException($"Invalid amount for {command.EntryType} entries; expected positive.");
        }

        // The semantic identity of this request: identical payloads must replay identically.
        var requestHash = ComputeRequestHash(command, amount.AmountMinor);

        for (var attempt = 0; ; attempt++)
        {
            var replayed = await TryReplayExistingEntryAsync(command, requestHash, amount.Currency, cancellationToken);
            if (replayed is not null)
            {
                return replayed;
            }

            var wallet = await RequireWalletAsync(command.OwnerUserId, amount.Currency, cancellationToken);

            var entryAmount = signedCredit switch
            {
                true => amount,
                false => amount.Negate(),
                _ => amount, // adjustment: sign comes from the command itself
            };

            long resultingBalance;
            if (entryAmount.AmountMinor >= 0)
            {
                resultingBalance = wallet.Credit(entryAmount, command.EntryType, DateTimeOffset.UtcNow);
            }
            else
            {
                if (!wallet.TryDebit(entryAmount.Negate(), DateTimeOffset.UtcNow, out resultingBalance))
                {
                    return MutationOutcome.InsufficientFunds(wallet.BalanceMinor, wallet.Currency);
                }
            }

            ledger.Append(LedgerEntry.Append(
                wallet.Id,
                command.EntryType,
                entryAmount,
                command.IdempotencyKey,
                requestHash,
                resultingBalance,
                DateTimeOffset.UtcNow,
                command.ReferenceType,
                command.ReferenceId));

            Collect(
                entryAmount.AmountMinor >= 0 ? "wallet.credited" : "wallet.debited",
                new
                {
                    walletId = wallet.Id,
                    entryType = command.EntryType.ToString(),
                    amountMinor = Math.Abs(entryAmount.AmountMinor),
                    balanceAfterMinor = resultingBalance,
                    referenceType = command.ReferenceType,
                    referenceId = command.ReferenceId,
                },
                command.CorrelationId);

            try
            {
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return MutationOutcome.Applied(resultingBalance, wallet.Currency);
            }
            catch (WalletConcurrencyConflictException) when (attempt < MaxConcurrencyRetries)
            {
                // Another writer moved this wallet first: drop stale work, back off, reload fresh state.
                await unitOfWork.ResetAsync(cancellationToken);
                await BackOffAsync(attempt, cancellationToken);
            }
            catch (IdempotencyKeyConflictException) when (attempt < MaxConcurrencyRetries)
            {
                // A concurrent twin claimed the key: drop our duplicate and surface the committed outcome.
                await unitOfWork.ResetAsync(cancellationToken);
                await BackOffAsync(attempt, cancellationToken);
                var winner = await ledger.FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
                if (winner is not null)
                {
                    return await ResolveReplayAsync(winner, requestHash, command.CorrelationId, cancellationToken);
                }
            }
        }
    }

    /// <summary>Stepped backoff spreads hot-wallet writers so retries converge instead of thrashing.</summary>
    private static Task BackOffAsync(int attempt, CancellationToken cancellationToken) =>
        Task.Delay(TimeSpan.FromMilliseconds(20 * (attempt + 1)), cancellationToken);

    private async Task<Wallet> RequireWalletAsync(Guid ownerUserId, string currency, CancellationToken cancellationToken)
    {
        var userWallet = await wallets.FindForUserAsync(ownerUserId, currency, cancellationToken);
        return userWallet ?? throw new InvalidOperationException("Wallet does not exist for this user.");
    }

    /// <summary>
    /// M05-004 idempotency contract. Same key + same semantic payload → replay
    /// the committed outcome; same key + different payload → reject and audit.
    /// </summary>
    private async Task<MutationOutcome?> TryReplayExistingEntryAsync(
        WalletMutationCommand command,
        string requestHash,
        string currency,
        CancellationToken cancellationToken)
    {
        var existingEntry = await ledger.FindByIdempotencyKeyAsync(command.IdempotencyKey, cancellationToken);
        if (existingEntry is null)
        {
            return null;
        }

        if (!string.Equals(existingEntry.RequestHash, requestHash, StringComparison.Ordinal))
        {
            Collect("wallet.idempotency_conflict", new
            {
                idempotencyKey = command.IdempotencyKey,
                storedRequestHash = existingEntry.RequestHash,
                incomingRequestHash = requestHash,
            }, command.CorrelationId);
            await unitOfWork.SaveChangesAsync(cancellationToken); // persist the audit trail

            return new MutationOutcome(false, 0, currency, ReplayedExistingEntry: false)
            {
                FailureReason = "idempotency_conflict",
            };
        }

        var entryCurrency = currency;
        if (entryCurrency != DefaultCurrency)
        {
            var wallet = await wallets.FindByIdAsync(existingEntry.WalletId, cancellationToken);
            entryCurrency = wallet?.Currency ?? entryCurrency;
        }

        return MutationOutcome.Replayed(existingEntry.ResultingBalanceMinor, entryCurrency);
    }

    private async Task<MutationOutcome> ResolveReplayAsync(
        LedgerEntry winner,
        string requestHash,
        string? correlationId,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(winner.RequestHash, requestHash, StringComparison.Ordinal))
        {
            Collect("wallet.idempotency_conflict", new
            {
                idempotencyKey = winner.IdempotencyKey,
                storedRequestHash = winner.RequestHash,
                incomingRequestHash = requestHash,
            }, correlationId);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return new MutationOutcome(false, 0, winner.Currency, ReplayedExistingEntry: false)
            {
                FailureReason = "idempotency_conflict",
            };
        }

        var wallet = await wallets.FindByIdAsync(winner.WalletId, cancellationToken);
        return MutationOutcome.Replayed(winner.ResultingBalanceMinor, wallet?.Currency ?? DefaultCurrency);
    }

    /// <summary>Canonical semantic-request digest: key-scoped payload hash over type/amount/currency/reference.</summary>
    private static string ComputeRequestHash(WalletMutationCommand command, long amountMinor)
    {
        var canonicalPayload = string.Create(CultureInfo.InvariantCulture,
            $"{command.EntryType}|{amountMinor}|{command.Currency ?? DefaultCurrency}|{command.ReferenceType?.ToLowerInvariant() ?? ""}|{command.ReferenceId?.ToString("D") ?? ""}");
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalPayload));
        return Convert.ToHexString(digest).ToLowerInvariant();
    }

    private void Collect(string type, object payload, string? correlationId) =>
        outbox.Collect(type, JsonSerializer.Serialize(payload, CatalogAdminService.PayloadOptions), correlationId);
}
