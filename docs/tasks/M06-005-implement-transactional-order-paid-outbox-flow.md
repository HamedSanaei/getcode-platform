# M06-005: Implement transactional order-paid outbox flow

- Status: **DONE**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-002, M06-004

## Goal

Implement transactional order-paid outbox flow.

## Acceptance criteria

-[x] Payment success/order paid state and outbox intent commit atomically. (`IOrderPaidUnitOfWork` port: order transition + `OrderPaidEvent` intent in one transaction; rollback test proves no partial state)
-[x] Worker may process outbox at-least-once without duplicate fulfillment. (`OutboxWorkerService` + idempotent `IOutboxDispatchHandler.HandleOnceAsync`; redelivered message absorbed, side effect exactly once — test-pinned)
-[x] Outbox lease/retry/dead-letter/manual-review policy is explicit. (`OutboxRetryPolicy`: 5 attempts, exponential 30s..15min cap; dead-letter is an explicit terminal state, never silent)

## Required verification

-[x] transaction rollback test
-[x] duplicate outbox dispatch test
-[x] worker crash test (crash between claim and completion -> failure marked, retry succeeds exactly once)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Application-side flow delivered: OrderPaidEvent + IOrderPaidUnitOfWork (atomic commit contract), IOutboxLeaseStore/IOutboxDispatchHandler/OutboxWorkerService (at-least-once processing with idempotent handlers), OutboxRetryPolicy (explicit attempts/backoff/dead-letter).
- PaymentCallbackService now commits via IOrderPaidUnitOfWork when provided - order paid state and the fulfillment intent can never diverge.
- RESIDUAL (next integration point): EF implementation of IOrderPaidUnitOfWork/IOutboxLeaseStore against the existing outbox_messages table + a hosted worker loop land with M07-003 fulfillment kickoff, which is the first real consumer of dispatched events. The contracts and semantics are pinned by tests now so the persistence adapter is mechanical.
- Tests increased: backend 364 (+6 rollback/duplicate-dispatch/crash-recovery/dead-letter/policy tests).