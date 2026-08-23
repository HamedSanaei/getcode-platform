# M07-004: Implement expiry/cancel/refund compensation workflow

- Status: **TODO**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-003, M05-004

## Goal

Implement expiry/cancel/refund compensation workflow.

## Acceptance criteria

- [ ] Expiry/cancel races are explicit state transitions.
- [ ] Refund/credit occurs only under defined policy and is idempotent.
- [ ] Provider cancellation failure/ambiguity enters reconciliation rather than silently refunding incorrectly.

## Required verification

- [ ] race tests
- [ ] double refund prevention tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
