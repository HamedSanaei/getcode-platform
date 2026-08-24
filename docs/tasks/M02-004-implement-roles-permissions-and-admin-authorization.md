# M02-004: Implement roles/permissions and admin authorization

- Status: **DONE**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-001

## Goal

Implement roles/permissions and admin authorization.

## Acceptance criteria

- [x] Permissions such as orders.read/refund, pricing.manage, providers.manage, wallet.adjust are policy-based.
- [x] Admin authorization is server-side and deny-by-default.
- [x] Privilege changes are audit events.

## Required verification

- [x] authorization matrix tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Domain/Authorization/Role.cs`: `PermissionCatalog` (canonical scopes: orders.read/refund, pricing.manage, providers.manage, wallet.adjust) + `Role` aggregate — kebab-case key validation, permission grant/revoke restricted to registered scopes (deny-by-default extends to definitions), idempotent mutations emitting `RolePermissionsChanged`.
  - `AuthorizationEvents.cs`: `RoleRegistered`, `RolePermissionsChanged` domain events.
  - `GetCode.Application/Authorization/*`: ports (`IRoleRepository`, `IUserRoleRepository`, `IAuthorizationService`); `AuthorizationAdminService` (create role, change permission, set user role by email, effective-permission lookup) mirroring every privilege change into the outbox (`authz.role.created`, `authz.role.permissions_changed`, `authz.user.role_changed`); `EffectiveAuthorizationService` resolving deny-by-default union of assigned roles.
  - `GetCode.Persistence/Authorization/*`: `roles` table (ux key, jsonb permissions collection) + `user_roles` join table (pk userId+roleId); repositories; DI wiring.
  - Migration `20260824103523_AddAuthorization`; Program.cs service registrations.
  - Tests: `UnitTests/RoleTests.cs` (key normalization, unknown-permission rejection, idempotent event emission, catalog contents) + `IntegrationTests/AuthorizationMatrixTests.cs` (subject×permission matrix across four users/three roles, revocation propagation, unassignment idempotency, unknown role/user rejection, duplicate-role rejection, outbox audit assertions).
- Decisions/assumptions:
  - Permissions are a closed catalog: granting an unregistered string throws rather than inventing scopes ad hoc.
  - Role deletion is out of scope this task; system roles are flagged for bootstrap admin use.
  - HTTP enforcement middleware/policy endpoints arrive with M02-002/M02-003 sessions; this task delivers the model, resolution and audit trail server-side.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **181 tests green** (UnitTests 109, IntegrationTests 21 incl. new matrix test).
- Migration/config/operations impact: migration `AddAuthorization` adds `roles` and `user_roles`; no config changes; no secrets.
- Residual risk: no HTTP surface exposes authorization yet (by design until session strategy lands); audit events currently rely on the same outbox dispatcher as catalog events (worker dispatch arrives with M06+ flows).
- Next unblocked tasks: M05-003 (wallet and immutable ledger) is fully unblocked; M10-004 (backup/restore runbooks) also eligible but lower value before order flows exist.
