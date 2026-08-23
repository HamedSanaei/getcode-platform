# M06-001: Implement Order aggregate and explicit state machine

- Status: **TODO**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M05-002

## Goal

Implement Order aggregate and explicit state machine.

## Acceptance criteria

- [ ] Allowed transitions are explicit; invalid transitions fail deterministically.
- [ ] Order stores immutable commercial snapshot/reference needed for support/audit.
- [ ] State names separate payment from fulfillment outcomes.

## Required verification

- [ ] state transition matrix tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
