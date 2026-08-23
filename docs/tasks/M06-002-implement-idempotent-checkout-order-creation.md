# M06-002: Implement idempotent checkout/order creation

- Status: **TODO**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-001, M05-004

## Goal

Implement idempotent checkout/order creation.

## Acceptance criteria

- [ ] Duplicate client submit cannot create/pay two orders.
- [ ] Request idempotency is scoped/authenticated and persisted durably.
- [ ] Order creation does not call external provider while holding DB transaction.

## Required verification

- [ ] duplicate concurrent request integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
