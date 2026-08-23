# M02-001: Implement identity model and authentication service

- Status: **TODO**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement identity model and authentication service.

## Acceptance criteria

- [ ] Identity model owns user auth without coupling to wallet/order entities.
- [ ] Password/credential policy and account lifecycle are tested.
- [ ] Sensitive auth events are audited without secret logging.

## Required verification

- [ ] auth unit/integration tests
- [ ] security review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
