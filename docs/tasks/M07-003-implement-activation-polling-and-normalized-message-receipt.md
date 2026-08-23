# M07-003: Implement activation polling and normalized message receipt

- Status: **TODO**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-002

## Goal

Implement activation polling and normalized message receipt.

## Acceptance criteria

- [ ] Worker polls with bounded schedule/backoff and provider rate constraints.
- [ ] Message receipt is deduplicated and state transition is idempotent.
- [ ] OTP/raw SMS does not enter general logs.

## Required verification

- [ ] poll/retry/dedup/redaction tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
