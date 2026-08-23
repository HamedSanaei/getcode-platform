# M06-004: Implement first payment gateway and verified callback

- Status: **TODO**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-003

## Goal

Implement first payment gateway and verified callback.

## Acceptance criteria

- [ ] Callback authenticity and amount/order/currency are verified server-side.
- [ ] Duplicate callback is idempotent; replay/invalid signature is rejected/audited.
- [ ] Redirect uses persisted allow-listed Site Context, not arbitrary query URL.

## Required verification

- [ ] signature/replay/duplicate/amount mismatch tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
