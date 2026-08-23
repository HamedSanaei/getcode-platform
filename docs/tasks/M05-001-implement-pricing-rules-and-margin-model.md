# M05-001: Implement pricing rules and margin model

- Status: **TODO**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M03-002, M04-004

## Goal

Implement pricing rules and margin model.

## Acceptance criteria

- [ ] Authoritative sell price is computed server-side from explicit rules.
- [ ] Rounding/currency/margin boundaries are documented and tested.
- [ ] Historical order price does not change when a rule changes.

## Required verification

- [ ] pricing boundary/property tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
