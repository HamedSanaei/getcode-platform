# M11-003: Add a second fulfillment product type

- Status: **TODO**
- Milestone: **M11**
- Priority: **P2**
- Depends on: M10-006

## Goal

Add a second fulfillment product type.

## Acceptance criteria

- [ ] New fulfillment type reuses Commerce Core without special-casing virtual numbers in Order/Wallet.
- [ ] Fulfillment routing is extension-based and tested.
- [ ] Architecture review confirms original separation held.

## Required verification

- [ ] new fulfillment contract/E2E tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
