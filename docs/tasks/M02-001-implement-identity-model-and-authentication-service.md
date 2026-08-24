# M02-001: Implement identity model and authentication service

- Status: **DONE**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement identity model and authentication service.

## Acceptance criteria

- [x] Identity model owns user auth without coupling to wallet/order entities.
- [x] Password/credential policy and account lifecycle are tested.
- [x] Sensitive auth events are audited without secret logging.

## Required verification

- [x] auth unit/integration tests
- [x] security review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Domain/Identity/`: `User` aggregate (registration factory, temporary-lockout state machine, permanent lock/unlock/disable lifecycle, password-hash rotation), `CredentialPolicy` (product constants), `PasswordPolicy` (pure quality checks incl. sequence/repeat rejection), `EmailNormalizer`, seven identity domain events.
  - `GetCode.Application/Identity/`: ports `IPasswordHasher` / `IUserRepository` / `IIdentityAuditTrail`, `IdentityService` (register/authenticate use cases), `IdentityRuleViolationException`.
  - `GetCode.Infrastructure/Identity/Pbkdf2PasswordHasher.cs`: PBKDF2-HMACSHA512 (210k iterations, 16B salt, 64B subkey), `FixedTimeEquals` verification, rehash-on-login flag; BCL-only, no new package.
  - `GetCode.Persistence/Identity/`: `UserConfiguration` (unique index on normalized_email), `identity_audit_events` table (jsonb details), `UserRepository`, `IdentityAuditTrail` with forbidden-key refusal (`AuditRedaction.cs`); migration `20260824091620_AddIdentity`.
  - Composition root registers `IdentityService`; Infrastructure DI registers hasher + policy.
  - Tests: `UnitTests/Identity/UserTests.cs`, `PasswordPolicyTests`, `EmailNormalizerTests`, `IdentityServiceTests` (fakes), `Pbkdf2PasswordHasherTests`; `IntegrationTests/IdentityIntegrationTests.cs` (real PostgreSQL via production DI).
- Decisions/assumptions:
  - No public HTTP endpoints yet — session/token strategy is M02-002 and CSRF/CORS is M02-003; shipping unauthenticated login endpoints before those would violate the security baseline. Integration tests drive the production composition root directly.
  - Unknown account burns comparable PBKDF2 time (constant dummy hash) so latency does not leak account existence; invalid-credentials result is shared for both cases.
  - Temporary lockout (5 failures / 15 min window → 15 min lock) is durable DB state, verified to survive scope/process restarts. Permanent lock is a distinct admin action with mandatory reason, unlockable only explicitly.
  - Audit events persist structured metadata only; the persistence adapter refuses obvious sensitive keys as defense in depth.
- Verification commands: `dotnet format GetCode.sln --verify-no-changes` (0 changes); `dotnet build GetCode.sln -c Release --no-restore` (0 warnings/0 errors); full suite **95 tests green** (UnitTests 44, IntegrationTests 13, ObservabilityTests 30, ArchitectureTests 8). Security review checklist applied per `.agents/prompts/reviewers.md`: no secrets in audit rows/logs (asserted by tests), constant-time hash compare, lockout anti-brute-force, no user enumeration.
- Migration/config/operations impact: new tables `users`, `identity_audit_events`; expand-only migration, no data backfill needed. New env vars: none (policy constants are code; rate limiting at the edge arrives with M02-002/M10).
- Residual risk: email verification/OTP flows intentionally deferred until activation milestone; password breach-corpus check not yet integrated (policy covers structure only) — revisit at security hardening M10.
- Next unblocked tasks: M02-002 (session/token strategy), M02-003 (CSRF/CORS), M03-001 (canonical catalog).
