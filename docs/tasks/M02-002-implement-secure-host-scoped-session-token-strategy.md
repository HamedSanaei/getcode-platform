# M02-002: Implement secure host-scoped session/token strategy

- Status: **TODO**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-001, M01-006

## Goal

Implement secure host-scoped session/token strategy.

## Acceptance criteria

- [ ] Sessions work independently on both root domains against shared identity.
- [ ] Cookie flags/lifetime/rotation/revocation are documented/tested.
- [ ] No attempt is made to share a cookie across unrelated root domains.

## Required verification

- [ ] browser session tests on two hostnames

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
