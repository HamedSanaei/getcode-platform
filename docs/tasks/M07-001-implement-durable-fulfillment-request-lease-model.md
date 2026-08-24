# M07-001: Implement durable fulfillment request/lease model

- Status: **DONE**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M06-005, M04-006

## Goal

Implement durable fulfillment request/lease model.

## Acceptance criteria

- [x] Worker claims work with lease/ownership semantics safe for future multiple workers. (atomic conditional UPDATE..RETURNING with FOR UPDATE SKIP LOCKED; integration test proves two racing workers own disjoint jobs)
- [x] Job state survives process/container restart. (persisted `fulfillment_requests` rows + AddFulfillmentRequests migration; restart test re-opens the factory over the same database and recovers the same job)
- [x] Stale leases can be recovered without double side effects. (`RecoverExpiredLeasesAsync` returns expired leases to Pending; recovery is only a re-queue — side effects belong to the downstream handler, guarded by M06-005 idempotent dispatch)

## Required verification

- [x] multi-worker claim integration tests
- [x] crash recovery test

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Application port `IFulfillmentJobStore` + explicit `FulfillmentLeasePolicy` (2-minute leases, 5 attempts); states Pending/Leased/Completed/Failed/DeadLettered.
- Persistence: `FulfillmentRequestRecord` mapped to `fulfillment_requests` (unique per order), `FulfillmentJobStore` with single-statement atomic claim (FOR UPDATE SKIP LOCKED) covering pending-first then expired-lease recovery; FailAsync enforces dead-letter at MaxAttempts; registered scoped in Persistence DI.
- Integration tests (real PostgreSQL): concurrent workers claim disjoint jobs; queue drains to null without duplicate ownership; container-death restart recovers the crashed job for a new owner; enqueue idempotent per order.
- Residual: the fulfillment HANDLER that consumes claimed jobs lands as M07-002 reservation orchestration.
- Tests increased: backend 370 (+4).
