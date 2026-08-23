# M04-003: Implement provider health and balance observation

- Status: **TODO**
- Milestone: **M04**
- Priority: **P1**
- Depends on: M04-002

## Goal

Implement provider health and balance observation.

## Acceptance criteria

- [ ] Health/balance observations are timestamped and normalized.
- [ ] Provider account balance is operational data and never customer wallet truth.
- [ ] Polling has rate/backoff limits and metrics.

## Required verification

- [ ] worker scheduling tests
- [ ] provider fault tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
