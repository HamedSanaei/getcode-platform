# M05-004: Implement idempotent debit/credit/refund primitives

- Status: **TODO**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M05-003

## Goal

Implement idempotent debit/credit/refund primitives.

## Acceptance criteria

- [ ] Same idempotency key + same semantic request returns same result.
- [ ] Same key + conflicting payload is rejected/audited.
- [ ] Crash/retry does not create duplicate ledger entries.

## Required verification

- [ ] duplicate/concurrency/crash-retry tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
