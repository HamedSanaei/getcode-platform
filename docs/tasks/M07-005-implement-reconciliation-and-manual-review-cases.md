# M07-005: Implement reconciliation and Manual Review cases

- Status: **TODO**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-004

## Goal

Implement reconciliation and Manual Review cases.

## Acceptance criteria

- [ ] System can identify stuck/ambiguous payment/provider/fulfillment records by rule.
- [ ] Manual Review stores evidence/reason/status without exposing secrets.
- [ ] Admin action resolves through domain/application commands, not direct DB edits.

## Required verification

- [ ] reconciliation rule tests
- [ ] manual resolution audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
