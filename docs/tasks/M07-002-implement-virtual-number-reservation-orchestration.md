# M07-002: Implement virtual-number reservation orchestration

- Status: **TODO**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-001

## Goal

Implement virtual-number reservation orchestration.

## Acceptance criteria

- [ ] Router/provider reservation is invoked outside DB transaction with durable attempt identity.
- [ ] Result is reconciled idempotently into activation/order state.
- [ ] Ambiguous provider result never triggers unsafe blind duplicate purchase.

## Required verification

- [ ] reserve success/timeout/ambiguous tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
