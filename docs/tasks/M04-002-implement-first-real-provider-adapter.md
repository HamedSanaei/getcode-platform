# M04-002: Implement first real provider adapter

- Status: **DONE**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-001

## Goal

Implement first real provider adapter.

## Acceptance criteria

- [x] Provider HTTP client has explicit timeout/auth/user-agent and cancellation.
- [x] Vendor DTOs and IDs are contained inside its Infrastructure folder (`Infrastructure/Providers/FiveSim`, internal wire models).
- [x] Adapter passes the shared contract suite plus vendor-specific mapping/redaction tests against a stateful stubbed HTTP handler (32 provider-contract tests total).

## Required verification

- [x] contract tests
- [x] HTTP mapping tests (failure matrix: no-inventory, insufficient balance, invalid country/service/operator, auth failure, 429, 5xx, plain-text-over-200 errors, malformed bodies)
- [x] redaction tests (bearer token only in headers, never in URLs/results; safe error tokens are stable ASCII, never raw provider text)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Product decision recorded: first real virtual-number provider = **5SIM** (current protocol). Adapter: `Infrastructure/Providers/FiveSim` implementing `IVirtualNumberProvider`; anti-corruption layer strict — internal wire DTOs never escape; canonical country keys map to vendor country names via configuration.
- Purchase safety: 5SIM buy has no idempotency key. Transport-level failure AFTER send (timeout/connection reset) surfaces as the NEW canonical `ProviderErrorCode.AmbiguousOutcome` ("ambiguous-purchase"); the idempotency key is recorded so same-key retries are refused ("duplicate-purchase-risk") until reconciliation proves non-duplication (M04-006 owns durable reconciliation). Definitive server responses map through the failure table instead.
- Failure model: success / no-inventory / insufficient-balance / invalid-country|service|operator / auth-failed / rate-limited / transient-http(5xx) / timeout / malformed-response / activation-not-found / cancelled / expired / sms-received / completed / ambiguous-purchase — each pinned by tests with stable SafeErrorCode tokens; raw provider text never reaches callers.
- Configuration: `FiveSim:Enabled`, `ApiToken` (secret), `BaseUrl`, `TimeoutSeconds` (explicit HttpClient timeout), `CountryMap`, `DefaultOperator`. Registered only when enabled; token lives solely in request Authorization headers.
- Balance observation (`GetBalanceAsync`) exposed on the concrete type for M04-003 without polluting the canonical port.
- Live verification: externally blocked (requires funded 5SIM account credentials); everything else runs offline/stubbed by design.