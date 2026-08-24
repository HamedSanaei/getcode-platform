# M07-004: Implement expiry/cancel/refund compensation workflow

- Status: **DONE**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-003, M05-004

## Goal

Implement expiry/cancel/refund compensation workflow.

## Acceptance criteria

-[x] Expiry/cancel races are explicit state transitions. (the ORDER STATE MACHINE arbitrates before money moves: a concurrent message arrival makes the transitions throw and surfaces RaceLostMessageArrived with zero credit applied — test-pinned)
-[x] Refund/credit occurs only under defined policy and is idempotent. (policy: captured funds + uncompleted fulfillment; idempotency key `refund:{orderId}` through the M05-004 ledger; replay returns AlreadyRefunded, exactly one real credit ever)
-[x] Provider cancellation failure/ambiguity enters reconciliation rather than silently refunding incorrectly. (ambiguous/failed cancel -> ReconciliationRequired: no credit, order untouched; completed orders rejected outright without provider contact)

## Required verification

[x] race tests
[x] double refund prevention tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Fulfillment/CompensationWorkflow.cs`: CancelAndRefundAsync with ordering guards -> definitive provider cancel -> STATE-MACHINE ARBITRATION -> ledger-idempotent credit -> persist. Ambiguous cancels NEVER auto-refund. Wallet credit failure leaves durable state untouched (retryable).
- Tests use the REAL WalletService over in-memory ledger fakes (replay semantics exercised end to end).
- Residual: worker-loop wiring that feeds expired reservations into this workflow lands with M07-005 reconciliation sweep.
- Tests increased: backend 387 (+5 happy-path/double-refund/ambiguity/race/completed-rejection).