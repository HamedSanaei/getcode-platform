# M09-001: Implement admin shell and permission-aware navigation

- Status: **DONE**
- Milestone: **M09**
- Priority: **P0**
- Depends on: M02-004, M01-007

## Goal

Implement admin shell and permission-aware navigation.

## Penpot implementation reference

Map to the six `Admin / *` boards on `GetCode · 09 Admin`, including provider operations, canonical mapping, pricing, order/refund support and mobile manual review. Exact IDs and state requirements are in `design/handoff/PENPOT_PAGE_MAP.md`.

## Acceptance criteria

- [x] Admin UI is server-authorized; hidden navigation is not treated as authorization.
- [x] Penpot admin patterns cover shell states; data-bearing screens land with M09-002/003/004.
- [x] No dangerous actions exist in the shell yet; the confirmation/reason pattern is required by M09-002+ task contracts.

## Required verification

- [x] permission E2E
- [x] visual/accessibility tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Authorization architecture (backend-authoritative):
  - `PermissionCatalog.AdminAccess` ("admin.access") added as a canonical capability alongside the existing five — same deny-by-default registry.
  - `SessionAuthenticationHandler` ("Session" scheme): validates the host-scoped cookie via SessionService (+ICurrentSite) and issues a ClaimsPrincipal (NameIdentifier=userId, gc.session claim). Authentication only — it never decides capabilities.
  - `PermissionRequirement` + `PermissionAuthorizationHandler`: evaluates canonical permissions against `IAuthorizationService.HasPermissionAsync` (effective permissions of assigned roles). Policy "admin.access" = authenticated user + that requirement.
  - `/api/admin/*` group: `.RequireAuthorization("admin.access")` at the group level — every current and future admin endpoint inherits enforcement; controllers/endpoints never compare role names inline.
  - `GET /api/auth/principal` (authenticated): returns `{userId, roles[], permissions[]}` — stable role keys and canonical permission strings for UX/navigation. Explicitly NOT a security boundary.
- Admin shell (frontend):
  - `/admin/layout.tsx` renders `AdminGuard`: loading / anonymous sign-in prompt / permission-denied / shell states. The guard is UX only (client-side principal fetch); direct API calls stay protected server-side regardless.
  - Capability-filtered navigation (`ADMIN_NAV_ITEMS` × effective permissions): Overview (admin.access), Provider operations & Catalog mapping (providers.manage), Pricing (pricing.manage), Orders & refunds + Manual review (orders.read). RTL-first shell (`dir=rtl`), design-system SidebarItem primitives, responsive single-column collapse under 768px, robots noindex.
  - `/admin` overview page (board `…877889b74e32`) with loading/error/ready states over `GET /api/admin/overview`.
- Tests (counts increased: backend 217, frontend vitest 45, Playwright 40):
  - Integration `AdminAuthorizationTests` (3 tests): unauthenticated principal→401 and admin API→401; plain user→empty capability view AND admin API→403 on direct access; capable user→role key + admin.access exposed and admin API→200. Role seeded through the audited `AuthorizationAdminService`; role creation made idempotent against the shared fixture.
  - Vitest `AdminShell.test.tsx` (7): capability model invariants, per-principal nav filtering, anonymous/denied/capable guard states.
  - Playwright `admin.visual.spec.ts` (6 captures): overview (capable), limited principal, anonymous — desktop+mobile, internal API mocked at route layer.
- Penpot note: shell maps `GetCode · 09 Admin` overview board + permission-denied pattern; tables/filters/detail/audit/manual-review screens belong to M09-002/003/004 which inherit this enforcement and shell.
- Residual risk: none architectural. The SPA guard could be bypassed by a crafted client, by design — every /api/admin route rejects such callers (tested).
- Next unblocked task: M09-003 (catalog/provider mapping management UI) — deps M03-003 ✓ + M09-001 ✓.