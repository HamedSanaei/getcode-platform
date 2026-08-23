# M06-003: Define payment gateway port and fake adapter

- Status: **TODO**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-001

## Goal

Define payment gateway port and fake adapter.

## Acceptance criteria

- [ ] Application owns normalized payment intent/verification contract.
- [ ] Fake gateway supports success/failure/duplicate/replay scenarios.
- [ ] Gateway DTO/signatures remain Infrastructure-only.

## Required verification

- [ ] payment contract tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
