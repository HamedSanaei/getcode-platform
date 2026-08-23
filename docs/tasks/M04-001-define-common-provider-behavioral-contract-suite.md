# M04-001: Define common provider behavioral contract suite

- Status: **TODO**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M03-003, M00-005

## Goal

Define common provider behavioral contract suite.

## Acceptance criteria

- [ ] Reusable tests define search/reserve/status/cancel/error/timeout/cancellation behavior.
- [ ] Fake provider is deterministic and configurable for failure injection.
- [ ] Suite verifies no raw vendor payload leaks to canonical results/logs.

## Required verification

- [ ] provider contract test suite

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
