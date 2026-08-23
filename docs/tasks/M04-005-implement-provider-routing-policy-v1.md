# M04-005: Implement provider routing policy v1

- Status: **TODO**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-003, M04-004

## Goal

Implement provider routing policy v1.

## Acceptance criteria

- [ ] Routing is an isolated policy with deterministic inputs and decision reason.
- [ ] Price/availability/health can be considered without hard-coded provider branches in Orders.
- [ ] Decision emits safe structured telemetry.

## Required verification

- [ ] routing unit tests
- [ ] tie/failure tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
