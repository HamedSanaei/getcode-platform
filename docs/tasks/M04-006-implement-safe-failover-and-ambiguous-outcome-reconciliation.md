# M04-006: Implement safe failover and ambiguous-outcome reconciliation

- Status: **DONE**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-005

## Goal

Implement safe failover and ambiguous-outcome reconciliation.

## Acceptance criteria

-[x] Reserve attempt states distinguish definitely-not-applied/applied/ambiguous.
-[x] Blind retry/failover is forbidden for ambiguous non-idempotent outcomes. (ambiguous stops the trail immediately; blocked key refuses all later attempts until resolved)
-[x] Ambiguous cases enter Pending Manual Review; ops evidence (not-applied) resolves the entry and unblocks a fresh attempt.

## Required verification

-[x] timeout-after-send tests (single attempt, no failover, review entry created)
-[x] duplicate-reservation prevention tests (blocked key contacts no provider at all)
-[x] reconciliation tests (resolve unblocks; unknown/double resolution fail cleanly)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Providers/ProviderReservationOrchestrator.cs`: attempt-state machine over ordered routing candidates. Mapping: success=Applied; definitive provider refusals=DefinitelyNotApplied (failover allowed); adapter AmbiguousOutcome=Ambiguous -> immediate stop, PendingManualReview entry, key blocked for ALL further attempts.
- Duplicate-purchase prevention is structural: blocked keys contact zero providers (`duplicate-purchase-risk` without an HTTP call).
- Reconciliation v1 is in-process with explicit states PendingManualReview/ResolvedNotApplied + ResolveNotApplied(idempotencyKey); durable persistence of reconciliation entries lands with the M06 order aggregate (same transaction as order creation) - recorded as residual risk there.
- Telemetry: attempts counted on meter GetCode.ProviderReservation by outcome (applied/not-applied/ambiguous/duplicate-risk).
- Tests increased: backend 294 (+5).