# M05-004: Implement idempotent debit/credit/refund primitives

- Status: **DONE**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M05-003

## Goal

Implement idempotent debit/credit/refund primitives.

## Acceptance criteria

- [x] Same idempotency key + same semantic request returns same result.
- [x] Same key + conflicting payload is rejected/audited.
- [x] Crash/retry does not create duplicate ledger entries.

## Required verification

- [x] duplicate/concurrency/crash-retry tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `LedgerEntry` gained `RequestHash` (SHA-256 hex of the canonical semantic payload, validated at `Append`); persisted as `wallet_entries.request_hash` via migration `20260824111759_AddLedgerRequestHash`.
  - `WalletService`: computes a canonical request digest per mutation (`entryType|amountMinor|currency|referenceType|referenceId`, invariant culture); replay path compares stored vs incoming hash — equal → `MutationOutcome.Replayed`; different → rejected with `FailureReason = "idempotency_conflict"` AND an outbox audit event `wallet.idempotency_conflict` (persisted). The concurrent-key race path resolves through the same hash comparison.
  - `MutationOutcome` gained `FailureReason` (`insufficient_funds` | `idempotency_conflict`).
  - Tests: `Same_key_with_conflicting_payload_is_rejected_and_audited` (amount conflict + type conflict, ledger untouched, two audit events), `Crash_retry_after_commit_replays_instead_of_duplicating` (separate "host" scopes; retry replays committed entry, no duplicate), plus existing duplicate/concurrency suites re-verified green.
- Decisions/assumptions:
  - Conflicting-payload reuse returns a typed failure result rather than throwing, so callers can surface it deterministically; the audit event is the durable record for support/reconciliation.
  - The request hash covers semantic fields only (not timestamps or resulting balance), so legitimate retries hash identically across hosts.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **198 tests green**, three consecutive runs to confirm no flakes in the concurrency paths.
- Migration/config/operations impact: migration adds non-null `request_hash` to `wallet_entries` (fresh deploys only; feature shipped pre-production).
- Residual risk: hash is scoped per entry — cross-wallet key reuse with identical payload hashes still replays the original wallet's outcome; acceptable until multi-currency/multi-tenant keys are needed.
- Next unblocked tasks: M01-004 (Penpot design token bridge) is fully eligible; remaining M04/M06+ chains wait on provider credentials / gateway selection / upstream milestones.
