# M09-002: Implement provider operations dashboard

- Status: **TODO**
- Milestone: **M09**
- Priority: **P1**
- Depends on: M04-003, M09-001

## Goal

Implement provider operations dashboard.

## Acceptance criteria

- [ ] Shows normalized health/balance/latency/success observations without secrets.
- [ ] Provider enable/disable/routing controls are permissioned and audited.
- [ ] Metrics distinguish lack of data from failure.

## Required verification

- [ ] admin authorization tests
- [ ] provider control audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
