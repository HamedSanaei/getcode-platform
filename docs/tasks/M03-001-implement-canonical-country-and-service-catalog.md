# M03-001: Implement canonical Country and Service catalog

- Status: **DONE**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement canonical Country and Service catalog.

## Acceptance criteria

- [x] Country/service identities are GetCode-owned stable keys.
- [x] Localization/display metadata is separated from provider IDs.
- [x] Enable/disable/order changes are auditable/admin-ready.

## Required verification

- [x] domain tests
- [x] persistence integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Domain/Catalog/Catalog.cs`: `Country` aggregate (ISO 3166-1 alpha-2 `Code` as stable key, normalized on write), `Service` aggregate (`Slug` kebab-case stable key), shared `LocalizedCatalogName` value object (culture-normalized, length-capped) — display metadata is owned by GetCode, never provider-derived.
  - `GetCode.Domain/Catalog/CatalogEvents.cs`: upsert/availability/order domain events.
  - `GetCode.Application/Catalog/`: ports `ICountryRepository`, `IServiceRepository`, new reusable `IOutboxCollector` + `ICatalogUnitOfWork`; `CatalogAdminService` (idempotent upserts, availability toggles, display order) mirrors every aggregate event into the transactional outbox with the caller's correlation id; `CatalogQueryService` returns storefront read models in display order with culture-aware names.
  - `GetCode.Persistence/Catalog/`: direct EF mappings of the aggregates (`countries`, `services`, owned `country_localized_names` / `service_localized_names`), unique indexes on code/slug, repositories, outbox collector stamping trace context via `OutboxMessage.Create`; DI registrations; migration `20260824093919_AddCatalog`.
- Decisions/assumptions:
  - Stable keys are GetCode-owned (ISO country codes, kebab-case slugs); provider identifiers remain mappings for M03-003 and never enter these tables.
  - New catalog entries start disabled; enabling is an explicit audited admin action (idempotent re-toggles raise no duplicate events).
  - Audit trail = outbox rows (`catalog.country.*` / `catalog.service.*`) persisted in the same unit of work as the change, carrying W3C trace context — no separate audit table needed for catalog mutations.
  - Real catalog contents (which countries/services to sell) are product data and intentionally not seeded from code; admin upsert API is the seeding mechanism.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **130 tests green** (UnitTests 76 incl. catalog domain + admin service fakes, IntegrationTests 16 incl. catalog persistence/outbox/trace-context paths, ObservabilityTests 30, ArchitectureTests 8).
- Migration/config/operations impact: expand-only migration adding four tables; no env changes. Integration tests reset catalog tables at start because the collection fixture database persists between tests.
- Residual risk: localized-name coverage is data-dependent (only what admins enter); admin UI/authz for catalog management arrives with later milestones (commands exist behind DI today).
- Next unblocked tasks: M03-002 (Product/SKU model binding country+service), M03-004 partially blocked by read models needing SKUs; M02 track continues after design-gate tasks.
