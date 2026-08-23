# M10-001: Create critical-path browser E2E suite

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M08-004, M09-005

## Goal

Create critical-path browser E2E suite.

## Acceptance criteria

- [ ] E2E covers registration/login, browse, quote, pay/fake callback, fulfillment, OTP and refund/failure paths.
- [ ] Suite uses deterministic fake provider/payment adapters and runs in CI.
- [ ] At least one duplicate/retry/crash-style workflow is covered end-to-end.

## Required verification

- [ ] clean-environment E2E run

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
