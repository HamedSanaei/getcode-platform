# M08-004: Implement activation/OTP live experience

- Status: **TODO**
- Milestone: **M08**
- Priority: **P0**
- Depends on: M07-003, M08-003

## Goal

Implement activation/OTP live experience.

## Penpot implementation reference

Map to `Activation / Live / Desktop`, `Activation / Live / Mobile` and `Activation / State Gallery` on `GetCode · 07 Activation & OTP`. The six explicit states and board IDs are recorded in `design/handoff/PENPOT_PAGE_MAP.md`.

## Acceptance criteria

- [ ] Activation status updates efficiently without long blocking API request.
- [ ] Copy/display behavior follows product privacy/retention policy.
- [ ] Expiry/cancel/retry states are clearly represented and tested.

## Required verification

- [ ] activation E2E with fake provider
- [ ] timeout/cancel UX tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
