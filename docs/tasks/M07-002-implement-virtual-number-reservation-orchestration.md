# M07-002: Implement virtual-number reservation orchestration

- Status: **DONE**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-001

## Goal

Implement virtual-number reservation orchestration.

## Acceptance criteria

-[x] Router/provider reservation is invoked outside DB transaction with durable attempt identity. (provider call happens first; durable attempt identity is the job id: `fulfillment:{jobId}` — stable across restarts/retries)
-[x] Result reconciles idempotently into order state. (Applied -> StartFulfillment+MarkProviderReserved through explicit aggregate guards then CompleteAsync; already-reserved jobs reconcile as no-op)
-[x] Ambiguous provider result never triggers unsafe blind duplicate purchase. (ambiguous -> straight to DeadLettered manual review, zero further provider contact — test proves exactly ONE provider call across a restart re-run of the same job)

## Required verification

-[x] reserve success/timeout/ambiguous tests (+ definitive-failure retry path: back in Pending queue, next attempt succeeds)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Fulfillment/ReservationOrchestrationService.cs`: claimed job + routing candidates in -> provider reservation via M04-006 orchestrator -> idempotent reconciliation into the order aggregate. Applied completes the job; definitive failures release it for retry; ambiguous dead-letters immediately (manual review) with provably no second provider call.
- Orders arrive at fulfillment ALREADY paid (M06-005 flow); orchestration touches only fulfillment state.
- Residual: activation polling (M07-003) continues from ProviderOperationId recorded on the order.
- Tests increased: backend 373 (+3).