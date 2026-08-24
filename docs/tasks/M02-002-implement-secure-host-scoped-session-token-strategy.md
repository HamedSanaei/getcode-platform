# M02-002: Implement secure host-scoped session/token strategy

- Status: **DONE**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-001, M01-006

## Goal

Implement secure host-scoped session/token strategy.

## Acceptance criteria

- [x] Sessions work independently on both root domains against shared identity.
- [x] Cookie flags/lifetime/rotation/revocation are documented/tested.
- [x] No attempt is made to share a cookie across unrelated root domains.

## Required verification

- [x] browser session tests on two hostnames

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Domain/Sessions/`: `Session` aggregate — issue factory (validates site key presence, SHA-256 token-hash shape, positive lifetime), absolute expiry (`IsActive`), idempotent revocation with reason, `RotatedFromSessionId` lineage; `SessionIssued`/`SessionRevoked` domain events.
  - `GetCode.Application/Identity/SessionService.cs` + ports: `ISessionTokenProvider` (create/hash), `ISessionRepository`; use cases Issue / Validate (NotFound | SiteMismatch | Revoked | Expired | Success) / Rotate / Revoke / RevokeAllForUser; policy constants (`AbsoluteLifetime = 7 days`, site keys primary|pluspremium). Session lifecycle audited via `IIdentityAuditTrail` (`identity.session.*`, no token values ever recorded).
  - `GetCode.Infrastructure/Identity/CryptographicSessionTokens.cs`: 256-bit CSPRNG tokens as base64url cookie values; only SHA-256 hex digests are persisted.
  - `GetCode.Persistence/Identity/`: `sessions` table mapping (unique index on `token_hash`, composite index user+site), `SessionRepository`; migration `20260824120937_AddSessions`.
  - `GetCode.Api/Endpoints/AuthEndpoints.cs`: `/api/auth/login|logout|session|session/rotate`. Cookie names per site with the `__Host-` prefix (`__Host-gc_session`, `__Host-vpp_session`) — browsers refuse such cookies unless Secure is set, Path is "/", and NO Domain attribute exists, structurally forbidding parent-domain sharing between the two unrelated sites. Attributes: HttpOnly, Secure, SameSite=Lax, Max-Age=7d.
- Decisions/assumptions:
  - Server-side sessions in PostgreSQL (opaque token, hashed at rest) over JWT: trivial revocation, no secret rotation problem, satisfies "Postgres is source of truth".
  - Site scoping enforced twice: by per-host cookie naming AND server-side SiteKey comparison on every validation (raw token replay against the other host returns SiteMismatch → 401).
  - Each login issues a fresh session (multi-device friendly); rotation replaces exactly one exposed session; password-change wiring for RevokeAllForUser arrives with its own milestone (method ready).
- Verification: full suite 209 green (126 unit incl. 6 new session unit tests; 32 integration incl. 5 new HTTP flows: two-hostname independent sessions, cross-site replay rejection, logout revocation killing captured tokens, rotation semantics, forced-expiry rejection); format verify clean; build 0 warnings.
- Migration/config/operations impact: expand-only `AddSessions`; no new env vars (reuses SiteHosts configuration).
- Residual risk: CSRF hardening and rate limiting land with M02-003/M10-003; login remains un-throttled at the edge until then.
- Next unblocked tasks: M02-003 (CSRF/CORS/trusted redirects), M01-007 (visual harness), M10-004 (runbooks).