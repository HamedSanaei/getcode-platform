# M08-003: Implement customer order/dashboard UX

- Status: **TODO**
- Milestone: **M08**
- Priority: **P0**
- Depends on: M06-005, M01-007

## Goal

Implement customer order/dashboard UX.

## Penpot implementation reference

Map to the `Customer / *` boards on `GetCode · 06 Customer Dashboard`; see `design/handoff/PENPOT_PAGE_MAP.md` for board IDs and async/empty/error expectations.

## Acceptance criteria

- [ ] User can view only authorized orders/payments/ledger views.
- [ ] State labels are derived from server contracts and tolerate async progress.
- [ ] No sensitive debug/provider fields are exposed.

## Required verification

- [ ] authorization E2E
- [ ] visual regression

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
