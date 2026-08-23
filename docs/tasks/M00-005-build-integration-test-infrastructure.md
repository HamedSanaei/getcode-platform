# M00-005: Build integration-test infrastructure

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-003

## Goal

Build integration-test infrastructure.

## Acceptance criteria

- [ ] Integration tests can start isolated PostgreSQL/Redis dependencies without using developer data.
- [ ] Database is migrated/seeded per test suite deterministically.
- [ ] Tests cover transaction rollback and a representative persistence path.

## Required verification

- [ ] integration suite from clean environment

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
