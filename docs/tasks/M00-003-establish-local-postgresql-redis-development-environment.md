# M00-003: Establish local PostgreSQL/Redis development environment

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Establish local PostgreSQL/Redis development environment.

## Acceptance criteria

- [ ] Compose starts PostgreSQL and Redis with health checks and durable local volumes.
- [ ] Secrets remain local/env-only and examples are non-production.
- [ ] Developer README documents reset/start/stop procedures.

## Required verification

- [ ] docker compose config
- [ ] health checks

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
