# M04-001: Define common provider behavioral contract suite

- Status: **DONE**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M03-003, M00-005

## Goal

Define common provider behavioral contract suite.

## Acceptance criteria

- [x] Reusable tests define search/reserve/status/cancel/error/timeout/cancellation behavior.
- [x] Fake provider is deterministic and configurable for failure injection.
- [x] Suite verifies no raw vendor payload leaks to canonical results/logs.

## Required verification

- [x] provider contract test suite

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Infrastructure/Providers/Fake/FakeVirtualNumberProvider.cs`: rewritten as a fully scripted adapter — outcome queues per operation (`QueueSearchOutcome` etc.), idempotency-key replay for reservations, seeded reservations for status/cancel flows, simulated latency honoring cancellation tokens, deterministic clock injection via `IClock`. Now `public` so the contract project can drive it directly (production wiring unchanged: dev-only).
  - `tests/GetCode.ProviderContractTests/VirtualNumberProviderContractTests.cs`: reusable abstract suite every adapter must pass — search shape + sane timestamps, unavailability as result-not-exception with safe error tokens, reserve idempotency per key + E.164 shape + expiry sanity, status/cancel state transitions, unknown-operation safe failures, cancellation-token observance, timeout error mapping, and a JSON leakage guard asserting canonical results contain only contract fields (no smuggled vendor payloads).
  - `FakeVirtualNumberProviderTests.cs`: fake-specific determinism tests (queue drain order, injected failure codes, latency cancellation, seeded flows).
- Decisions/assumptions:
  - Contract rule chosen: adapters must surface caller cancellation via `OperationCanceledException`; upstream timeouts map to the canonical `Timeout` code with a short stable safe-token (≤64 chars, identifier-shaped) — never embedded vendor payloads.
  - Log-redaction of provider payloads remains enforced by the M00-007 redaction policy; this suite guards the result boundary (canonical records only).
- Verification commands: format verify clean; build 0 warnings/errors; full suite **176 tests green** (ProviderContractTests 13 new, UnitTests 105, IntegrationTests 20, ObservabilityTests 30, ArchitectureTests 8).
- Migration/config/operations impact: none.
- Residual risk: contract covers today's four port operations; balance/health observation (M04-003) extends the port and will extend this suite. Real adapters may need extra scripted cases (auth failures etc.) in their subclasses.
- Next unblocked tasks: M04-004 (offer normalization + availability cache) is unblocked by this suite; M04-002 waits on product-owner provider selection/credentials.
