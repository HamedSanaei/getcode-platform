# M03-004: Implement catalog/query read models

- Status: **DONE**
- Milestone: **M03**
- Priority: **P1**
- Depends on: M03-002, M03-003

## Goal

Implement catalog/query read models.

## Acceptance criteria

- [x] Public catalog queries avoid leaking disabled/internal/provider-only data.
- [x] Read paths are pagination/cache-ready without Redis becoming truth.
- [x] API contracts are documented via OpenAPI.

## Required verification

- [x] API contract tests
- [x] query integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Application/Common/PageRequest.cs`: clamped paging primitives (`PageRequest.Create`, max 100/page) + `PagedResult<T>` with totals.
  - `GetCode.Application/Catalog/*`: paged read methods on `CatalogQueryService` (countries/services, enabled only, deterministic display-order+key sort) and `ProductCatalogQueryService.ListOfferedSkusPagedAsync` (offered SKUs only, stable-key ordering).
  - `GetCode.Contracts/Catalog/CatalogResponses.cs`: `CountryResponse` / `ServiceResponse` / `OfferResponse` + generic `CatalogPageResponse<T>` envelope; the offer shape carries canonical fields only.
  - `GetCode.Api/Endpoints/CatalogEndpoints.cs`: anonymous same-origin `/api/catalog/{countries,services,offers}` GET endpoints with culture/page/pageSize query params, Produces-typed responses and OpenAPI summaries.
  - `Program.cs`: OpenAPI document now served in all environments at `/openapi/v1.json`.
- Decisions/assumptions:
  - First public HTTP surface of the platform: read-only catalog endpoints; auth/session endpoints still wait for M02-002/M02-003.
  - Cache-ready = deterministic ordering + page metadata + clamped sizes; any cache layer added later stays disposable by design (PostgreSQL remains truth).
  - Disabled countries/services and unoffered SKUs are excluded server-side; a raw-payload assertion test proves no provider/vendor tokens can appear in public offers.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **163 tests green** (UnitTests 105, IntegrationTests 20 incl. new API contract tests covering leakage guard, clamped pagination, localized cultures, OpenAPI path documentation).
- Migration/config/operations impact: none (no schema change); OpenAPI endpoint now public in production — intentional contract publication.
- Residual risk: ETag/conditional-request support not yet implemented (straightforward follow-up once CDN caching strategy is decided); rate limiting on these reads arrives with hardening M10 per baseline.
- Next unblocked tasks: M04-001 (common provider behavioral contract suite).
