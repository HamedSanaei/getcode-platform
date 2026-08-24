# M05-003: Implement wallet and immutable ledger

- Status: **DONE**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement wallet and immutable ledger.

## Acceptance criteria

- [x] Every wallet mutation produces a ledger entry with type/reference/idempotency identity.
- [x] Concurrent debits cannot overspend according to chosen transaction/locking strategy.
- [x] Adjustments/refunds are separate compensating entries, never history edits.

## Required verification

- [x] concurrent debit integration tests
- [x] ledger invariant tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Domain/Wallets/Money.cs`: minor-unit money value (`Money(AmountMinor, Currency)`, ISO-style 3-letter codes, int64 bounds, major/minor conversion with banker's rounding).
  - `Wallet.cs`: `Wallet` aggregate (owner+currency unique, projected `BalanceMinor`, Npgsql `xmin` optimistic-concurrency token, close lifecycle, `Credit`/`TryDebit` refusing negative balances by construction) and append-only `LedgerEntry` (signed amount, entry type, reference type/id, required idempotency key ≤128 chars, resulting-balance snapshot; deposits must be credits).
  - `WalletEvents.cs`: opened/credited/debited/adjusted/closed domain events.
  - `GetCode.Application/Wallets/*`: ports (`IWalletRepository`, `ILedgerRepository` append-only, `IWalletUnitOfWork` translating EF conflicts into app-level types), `WalletService` (idempotent open; Deposit/Purchase/Refund/Adjust flows; replay of duplicate idempotency keys without side effects; bounded retry with stepped backoff on concurrent writers; outbox events `wallet.opened|credited|debited`).
  - `GetCode.Persistence/Wallets/*`: `wallets` (ux owner+currency, xmin rowversion) and `wallet_entries` tables (ux idempotency key enforced by the database, FK to wallets restrict); repositories + unit-of-work adapter mapping DbUpdateConcurrencyException→conflict and 23505→idempotency conflict.
  - Migration `20260824105033_AddWallets`; DI registrations; Program.cs service registration.
  - Tests: `UnitTests/WalletTests.cs` (money validation/arithmetic, wallet credit/debit/refusal/close, ledger entry validation) + `IntegrationTests/WalletLedgerTests.cs` (deposit→purchase→refund chain with compensating entries, duplicate-key replay without double effect, insufficient-funds refusal, **8-way concurrent debit race: exactly 5 succeed, zero overspend, sum(entries)==balance invariant**, per-call isolated contexts).
- Decisions/assumptions:
  - Concurrency strategy: optimistic concurrency on the wallet row (xmin token) + retry-with-backoff in the use case; PostgreSQL remains truth (no Redis). Overspending is impossible by construction since balance checks and updates commit atomically.
  - Ledger rows are immutable by contract (no update/delete code paths); corrections are new compensating entries referencing the original order.
  - Single active wallet per user+currency; default currency USD until multi-currency pricing (M05-001/M05-005) demands otherwise.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **196 tests green** across three consecutive runs (UnitTests 120 incl. wallet suite, IntegrationTests 25 incl. concurrency matrix).
- Migration/config/operations impact: migration adds `wallets` + `wallet_entries`; no config/secrets.
- Residual risk: hot-wallet contention beyond 6 retries surfaces as an explicit exception for callers to queue/retry later (documented behavior); exchange-rate handling deferred to M05-005; wallet HTTP surface intentionally absent until session auth lands (M02-002).
- Next unblocked tasks: M05-004 (idempotent debit/credit/refund primitives on top of this ledger), M05-001 (pricing rules, needs M04-004 which waits on provider decision), M10-004 (runbooks).
