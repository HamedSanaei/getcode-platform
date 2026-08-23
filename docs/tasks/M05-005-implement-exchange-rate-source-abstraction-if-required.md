# M05-005: Implement exchange-rate source abstraction if required

- Status: **TODO**
- Milestone: **M05**
- Priority: **P2**
- Depends on: M05-001

## Goal

Implement exchange-rate source abstraction if required.

## Acceptance criteria

- [ ] Product decision determines supported currencies/provider cost currencies.
- [ ] If conversion is required, rates are timestamped, cached safely and snapshotted into quotes.
- [ ] External rate source cannot mutate historical order values.

## Required verification

- [ ] rate stale/failure tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
