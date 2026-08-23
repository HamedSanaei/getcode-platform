# M07-001: Implement durable fulfillment request/lease model

- Status: **TODO**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M06-005, M04-006

## Goal

Implement durable fulfillment request/lease model.

## Acceptance criteria

- [ ] Worker claims work with lease/ownership semantics safe for future multiple workers.
- [ ] Job state survives process/container restart.
- [ ] Stale leases can be recovered without double side effects.

## Required verification

- [ ] multi-worker claim integration tests
- [ ] crash recovery test

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
