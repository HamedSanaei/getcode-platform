# M06-005: Implement transactional order-paid outbox flow

- Status: **TODO**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-002, M06-004

## Goal

Implement transactional order-paid outbox flow.

## Acceptance criteria

- [ ] Payment success/order paid state and outbox intent commit atomically.
- [ ] Worker may process outbox at-least-once without duplicate fulfillment.
- [ ] Outbox lease/retry/dead-letter/manual-review policy is explicit.

## Required verification

- [ ] transaction rollback test
- [ ] duplicate outbox dispatch test
- [ ] worker crash test

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
