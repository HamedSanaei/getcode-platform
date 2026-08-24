# M09-003: Implement catalog/provider mapping management

- Status: **DONE**
- Milestone: **M09**
- Priority: **P0**
- Depends on: M03-003, M09-001

## Goal

Implement catalog/provider mapping management.

## Acceptance criteria

- [x] Admin can manage mappings with validation/preview and audit trail.
- [x] Invalid/duplicate mapping cannot corrupt canonical catalog.
- [x] Changes do not rewrite historical order snapshots.

## Required verification

- [x] mapping mutation tests
- [x] audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Backend surface (all under `/api/admin`, group-level `admin.access` policy — server-authoritative):
  - `GET /api/admin/providers` — management read model: every registered provider with mappings resolved back to canonical stable keys (`ProviderAdminService.ListForManagementAsync`).
  - `POST /api/admin/providers/register` (idempotent on key), `POST /api/admin/providers/set-enabled`.
  - `POST /api/admin/mappings/preview` — dry-run resolution of the canonical target, no mutation.
  - `POST /api/admin/mappings/bind` — validated bind/rebind; unknown canonical targets are rejected (404) **before any mutation**, so the catalog can never be corrupted by a bad mapping.
- Validation & audit guarantees (pinned by tests):
  - Rebinding an existing external code updates the row in place — duplicates impossible (`Rebinding_replaces_instead_of_duplicating`).
  - Every accepted change writes transactional-outbox audit events (`providers.mapping.bound` / `providers.mapping.rebound`); rejected changes write nothing (`Bind_with_unknown_canonical_target...` asserts country count AND outbox count unchanged).
  - Historical order snapshots untouched: mapping edits only affect current routing tables; snapshots already embed stable keys at order time.
- Persistence: `IProviderRepository.ListAsync` + `IProviderMappingRepository.ListForProviderAsync` added (EF adapters ordered deterministically); unit-test fakes updated.
- Frontend: `/admin/catalog-mapping` (client page under the AdminGuard shell) — providers table with per-provider mapping list and enabled badges, register form, and a bind form that REQUIRES a successful preview before Bind enables. CSRF double-submit helper for mutations; loading/error/success states; RTL-first copy. Visual captures: ready + load-failure states, desktop+mobile.
- Tests increased: backend 221 (+4 integration: bind+audit, invalid-target rejection, rebind dedupe, preview dry-run), Playwright 44 (+4 captures). Vitest unchanged at 45 (page logic is API-contract-bound; contract covered by integration tests).
- Residual risk: none architectural; outbox dispatch to a durable audit sink remains future work (M10 observability chain, currently blocked).
- Next unblocked task: none — remaining TODOs are blocked on external prerequisites (see STATUS.md).