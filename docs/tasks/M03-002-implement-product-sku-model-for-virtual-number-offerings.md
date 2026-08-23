# M03-002: Implement Product/SKU model for virtual-number offerings

- Status: **TODO**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M03-001

## Goal

Implement Product/SKU model for virtual-number offerings.

## Acceptance criteria

- [ ] SKU expresses canonical service/country/product type and commercial availability.
- [ ] Provider selection is not stored as the customer product identity.
- [ ] Model leaves room for future fulfillment types.

## Required verification

- [ ] domain invariant tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
