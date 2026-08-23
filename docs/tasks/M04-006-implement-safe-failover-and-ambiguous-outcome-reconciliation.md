# M04-006: Implement safe failover and ambiguous-outcome reconciliation

- Status: **TODO**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-005

## Goal

Implement safe failover and ambiguous-outcome reconciliation.

## Acceptance criteria

- [ ] Reserve attempt states distinguish definitely-not-applied/applied/ambiguous.
- [ ] Blind retry/failover is forbidden for ambiguous non-idempotent outcomes.
- [ ] Ambiguous cases can reconcile or enter Manual Review.

## Required verification

- [ ] timeout-after-send tests
- [ ] duplicate-reservation prevention tests
- [ ] reconciliation tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
