# M10-002: Run load/concurrency and provider-failure tests

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M10-001

## Goal

Run load/concurrency and provider-failure tests.

## Acceptance criteria

- [ ] Measure API/DB/worker behavior at defined target load.
- [ ] Exercise concurrent wallet/order requests and provider latency/timeouts.
- [ ] Record bottlenecks/SLO candidates before adding scaling infrastructure.

## Required verification

- [ ] load report
- [ ] failure injection report

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
