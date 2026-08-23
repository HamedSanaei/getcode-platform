# M11-002: Harden horizontal worker coordination

- Status: **TODO**
- Milestone: **M11**
- Priority: **P1**
- Depends on: M10-002

## Goal

Harden horizontal worker coordination.

## Acceptance criteria

- [ ] Multiple workers can claim outbox/fulfillment jobs without duplicate side effects.
- [ ] Lease/heartbeat/stale recovery behavior is load-tested.
- [ ] Scaling does not depend on in-process locks.

## Required verification

- [ ] multi-worker soak tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
