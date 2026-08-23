# M02-004: Implement roles/permissions and admin authorization

- Status: **TODO**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-001

## Goal

Implement roles/permissions and admin authorization.

## Acceptance criteria

- [ ] Permissions such as orders.read/refund, pricing.manage, providers.manage, wallet.adjust are policy-based.
- [ ] Admin authorization is server-side and deny-by-default.
- [ ] Privilege changes are audit events.

## Required verification

- [ ] authorization matrix tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
