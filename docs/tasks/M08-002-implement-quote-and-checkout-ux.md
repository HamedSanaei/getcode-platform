# M08-002: Implement quote and checkout UX

- Status: **TODO**
- Milestone: **M08**
- Priority: **P0**
- Depends on: M06-002, M08-001

## Goal

Implement quote and checkout UX.

## Penpot implementation reference

Map to `Checkout / Desktop`, `Checkout / Mobile` and `Payment / Results` on `GetCode · 05 Auth & Checkout`; see `design/handoff/PENPOT_PAGE_MAP.md` for IDs and state coverage.

## Acceptance criteria

- [ ] UX handles quote expiry/refresh, insufficient wallet, payment-required and duplicate-submit safely.
- [ ] Client never treats locally calculated price/payment as authoritative.
- [ ] Loading/error/retry states follow Penpot handoff.

## Required verification

- [ ] browser duplicate-submit test
- [ ] visual/accessibility tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
