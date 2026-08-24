# M07-003: Implement activation polling and normalized message receipt

- Status: **DONE**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-002

## Goal

Implement activation polling and normalized message receipt.

## Acceptance criteria

-[x] Worker polls with bounded schedule/backoff and provider rate constraints. (`ActivationPollingPolicy`: 10s base interval doubling to a 2min cap, MaxPolls=60, 25min wall deadline anchored at reservation time; RateLimited is a transient outcome, never an error storm)
-[x] Message receipt is deduplicated and state transition is idempotent. (Completed guard: repeated polls after completion are CompletedAlready/DuplicateReceipt no-ops through the aggregate guards)
-[x] OTP/raw SMS does not enter general logs. (`SafeSummary` emits presence+length only; test pins that the OTP digits and body words never appear in a log summary; raw body flows only to secure delivery)

## Required verification

[x] poll/retry/dedup/redaction tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Fulfillment/ActivationPolling.cs`: ActivationPollingPolicy (pure schedule math), ISmsBodyReader capability port (raw bodies only for authorized delivery, never logs), ActivationPollingService (one idempotent poll tick: Waiting / MessageRecorded / DuplicateReceipt / CompletedAlready / RateLimited / Exhausted / ProviderFailure) + GetCode.Fulfillment meter with per-outcome counters.
- Deadline is anchored at reservation time supplied by the caller (worker loop), not order creation.
- Residual: hosted worker loop wiring (lease job -> orchestration -> polling cadence) lands with the M07-004 compensation workflow integration; provider adapters implement ISmsBodyReader next to their IVirtualNumberProvider implementation.
- Tests increased: backend 382 (+9).