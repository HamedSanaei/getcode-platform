# M04-003: Implement provider health and balance observation

- Status: **DONE**
- Milestone: **M04**
- Priority: **P1**
- Depends on: M04-002

## Goal

Implement provider health and balance observation.

## Acceptance criteria

- [x] Health/balance observations are timestamped and normalized.
- [x] Provider account balance is operational data and never customer wallet truth (separate capability port `IProviderBalanceObserver`, read-model snapshots only, no wallet/ledger API touched).
- [x] Polling has rate/backoff limits and metrics (`ProviderPollingPolicy` 60s healthy interval, exponential backoff capped at 15min; `GetCode.ProviderHealth` meter counter with outcome attribute).

## Required verification

- [x] worker scheduling tests (`ProviderHealthAndPollingTests`: interval/backoff/cap/reset)
-[x] provider fault tests (fault streaks degrade→unreachable, throwing observers never kill polling)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Capability port `IProviderBalanceObserver` (Application) implemented by FiveSimVirtualNumberProvider via explicit interface mapping over its existing balance query — the canonical virtual-number port stays minimal.
- `ProviderHealthService`: latest timestamped normalized snapshot per provider (Healthy/Degraded/Unreachable by failure streak ≥3), fault-safe (adapter exceptions recorded as observer-exception faults), meter counter `provider.health.polls{outcome}` on meter `GetCode.ProviderHealth`. Snapshots are an in-memory read model; durable history intentionally deferred to M10-005 observability work.
- `ProviderHealthPollingWorker` (Worker host): per-provider "next due" scheduling driven by the pure `ProviderPollingPolicy` (unit-tested without hosting); first poll immediate; logs carry providerKey/outcome/failures only.
- Admin surface: `GET /api/admin/providers/health` under the existing admin.access group policy.
- Balance semantics: supplier telemetry for ops/low-balance alerts ONLY — no code path connects it to customer wallets/ledgers (AGENTS.md money rules); pinned by test.
- Tests increased: backend 276 (+7). Residual: durable observation history + alerting thresholds land with M10-005/M04-005.