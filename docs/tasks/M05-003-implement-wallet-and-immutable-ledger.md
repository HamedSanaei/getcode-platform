# M05-003: Implement wallet and immutable ledger

- Status: **TODO**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement wallet and immutable ledger.

## Acceptance criteria

- [ ] Every wallet mutation produces a ledger entry with type/reference/idempotency identity.
- [ ] Concurrent debits cannot overspend according to chosen transaction/locking strategy.
- [ ] Adjustments/refunds are separate compensating entries, never history edits.

## Required verification

- [ ] concurrent debit integration tests
- [ ] ledger invariant tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
