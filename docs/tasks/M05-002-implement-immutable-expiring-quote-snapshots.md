# M05-002: Implement immutable expiring quote snapshots

- Status: **DONE**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M05-001

## Goal

Implement immutable expiring quote snapshots.

## Acceptance criteria

-[x] Quote binds product, customer-visible price/currency and expiry/identity. (immutable `QuoteSnapshot` record: QuoteId/CountryKey/ServiceKey/ProductTypeKey/Amount/Currency/IssuedAtUtc/ExpiresAtUtc/RuleVersion)
-[x] Checkout rejects expired/tampered quote references and can refresh safely. (ValidateForCheckout distinguishes Valid/NotFound/Expired/Tampered; amount must equal the stored authoritative snapshot; Refresh issues a NEW quote id at current rules - history untouched)
-[x] Provider cost snapshot needed for operations is separated from customer price. (separate `ProviderCostTrace` record; structural test pins that QuoteSnapshot has zero cost/provider fields; API response never carries them)

## Required verification

-[x] quote expiry/tamper tests (fake-clock expiry transition, tampered amount, unknown id)
-[x] API integration tests (`POST /api/quotes` with CSRF handshake -> 201 customer view; GET valid 200 / tampered 409 / unknown 404; cost fields absent from response body)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Quotes/QuoteService.cs`: immutable snapshots + explicit validation outcomes + safe refresh; TTL via `Quotes:TtlSeconds` (default 300, clamped 5..86400).
- `Api/Endpoints/QuoteEndpoints.cs`: public anonymous group like catalog; POST issue (CSRF-protected write), GET {id}?expectedAmount= revalidation (200/404/409/410). Customer responses exclude provider-cost data entirely.
- Store is in-memory for this task by design; durable persistence joins the M06-002 checkout transaction where quotes are consumed atomically with order creation (residual recorded there).
- Integration-test note: browser-write endpoints require the CSRF handshake + https client base (factory.ClientOptions.BaseAddress) because __Host-/Secure cookies are enforced - mirrored from AdminAuthorizationTests pattern.
- Tests increased: backend 321 (+6 unit, +1 integration).