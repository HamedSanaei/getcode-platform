# M00-008: Add tracing/metrics foundation

- Status: **TODO**
- Milestone: **M00**
- Priority: **P1**
- Depends on: M00-007

## Goal

Add tracing/metrics foundation.

## Acceptance criteria

- [ ] OpenTelemetry-compatible ActivitySource/Meter naming convention is documented and wired.
- [ ] Trace/correlation context can flow API -> durable job/outbox metadata.
- [ ] No high-cardinality sensitive labels are used.

## Required verification

- [ ] trace propagation test
- [ ] metric naming review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
