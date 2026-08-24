# M03-003: Implement provider registry and canonical mappings

- Status: **DONE**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M03-001

## Goal

Implement provider registry and canonical mappings.

## Acceptance criteria

- [x] Provider registry has stable provider keys/capability metadata.
- [x] Country/service/product mapping belongs to provider capability, not Domain vendor fields.
- [x] Mapping changes are validated and auditable.

## Required verification

- [x] mapping tests
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
  - `GetCode.Domain/Providers/ProviderRegistry.cs`: `ProviderDefinition` aggregate (stable kebab-case `ProviderKey`, capability metadata for activation/rental support, starts disabled), `MappingKind` enum, `ProviderMapping` entity binding provider external codes to canonical ids (pure factory + `RebindTo`, no static state).
  - `GetCode.Application/Providers/ProviderAdminService.cs`: ports `IProviderRepository`/`IProviderMappingRepository` with routing-oriented `ResolveCanonicalIdAsync`; idempotent registration, validated mapping binds/re-binds against the live canonical catalog, audit into outbox (`providers.registered`, `providers.availability_changed`, `providers.mapping.bound|rebound`) carrying correlation ids.
  - `GetCode.Persistence`: `providers` (unique key) + `provider_mappings` (unique provider/kind/external-code) tables, repositories, DI, migration `AddProviderRegistry`.
- Decisions/assumptions:
  - Real provider list is a product decision still pending; this task delivers the registry mechanism only — providers are registered via admin commands when credentials/onboarding arrive (M04).
  - `CanonicalId` is polymorphic (country or service by kind): no DB-level FK possible, so integrity is enforced where the stable key resolves through its repository at bind time.
  - Provider external codes keep their original casing (some providers are case-sensitive); canonical keys are normalized as in M03-001.
  - Credentials never enter these tables — they live in secret storage at adapter configuration time (ADR-004 boundary unchanged).
- Verification commands: format verify clean; build 0 warnings/errors; full suite **161 tests green** (UnitTests 105 incl. registry/mapping invariants + admin fakes, IntegrationTests 18 incl. registry roundtrip with reverse resolution + outbox audit assertions).
- Migration/config/operations impact: expand-only migration adding two tables; no env changes.
- Residual risk: mapping coverage per real provider unknown until onboarding; capability model may grow (e.g., voice/SMS-only splits) — enum-style extension points kept minimal by design.
- Next unblocked tasks: M03-004 (catalog/query read models).
