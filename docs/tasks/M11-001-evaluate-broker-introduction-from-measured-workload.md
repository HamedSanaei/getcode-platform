# M11-001: Evaluate broker introduction from measured workload

- Status: **TODO**
- Milestone: **M11**
- Priority: **P2**
- Depends on: M10-002

## Goal

Evaluate broker introduction from measured workload.

## Acceptance criteria

- [ ] Decision uses measured outbox/worker throughput/latency/operational needs.
- [ ] If justified, write ADR and introduce broker behind messaging port with inbox/idempotent consumer semantics.
- [ ] If not justified, explicitly retain PostgreSQL worker model.

## Required verification

- [ ] benchmark/ADR review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
