# M02-003: Implement CSRF, CORS and trusted redirect policy

- Status: **DONE**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-002

## Goal

Implement CSRF, CORS and trusted redirect policy.

## Acceptance criteria

- [x] State-changing browser requests have a CSRF strategy compatible with chosen auth.
- [x] Credentialed CORS is allow-listed; same-origin remains default.
- [x] Return/redirect URLs are selected from Site Context allow-list.

## Required verification

- [x] CSRF negative tests
- [x] origin/redirect abuse tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `GetCode.Api/Middleware/BrowserWriteProtectionMiddleware.cs`: two-layer gate for non-safe `/api/*` requests — (1) Origin header (when present) must match the current site's public base URL authority+scheme; (2) ASP.NET Core antiforgery validation. Callback prefixes (`/api/callbacks`, `/api/webhooks`) are exempt because gateway authenticity comes from signatures (M06), not browser origin.
  - `Program.cs`: antiforgery configured with `HeaderName=X-XSRF-TOKEN`, cookie `__Host-xcsrf` (SameSite=Strict, SecurePolicy.Always, Path=/); CORS policy `browser` built from `Cors:AllowedOrigins` (array or comma-separated string) — empty config grants NOTHING cross-origin (no wildcard possible); middleware ordered CorrelationId → SiteHostResolution → UseCors → BrowserWriteProtection.
  - `GetCode.Api/Endpoints/AuthEndpoints.cs`: `GET /api/auth/csrf` issues the token pair (cookie + body token for the SPA to echo); `GET /api/auth/redirect-target?returnUrl=…` resolves through the trusted policy.
  - `GetCode.Application/SiteHosts/`: `ISiteCatalog` port + `TrustedRedirectResolver` — relative single-slash paths are absolutized on the current site; absolute URLs allowed only as exact matches of configured site bases (https only); foreign origins, scheme-relative `//host`, backslash tricks and http downgrades all collapse to the current site base.
  - `ConfiguredSiteCatalog.cs`: composition-root adapter from SiteHostOptions to ISiteCatalog.
  - Tests: `BrowserProtectionIntegrationTests` (5 tests: missing-token rejection, full csrf flow incl. forged-header and missing-header negatives, cross-site Origin rejection with valid token pair, credentialed-CORS allow-list grant/deny/default-deny, redirect allow-list matrix). Session tests updated to the browser contract (fetch CSRF pair before state-changing calls; https client so SecurePolicy.Always behaves as in production).
- Decisions/assumptions:
  - Double-submit antiforgery over custom synchronizer tokens: stateless pairing of `__Host-xcsrf` cookie with `X-XSRF-TOKEN` header, hardened by Origin checking; session cookie stays SameSite=Lax as depth, not primary defense.
  - Antiforgery cookie keeps HttpOnly=true: the SPA receives the request token in the response body — the cookie never needs to be JS-readable.
  - Same-origin remains the default browser path per ADR-006; CORS exists only for explicitly configured partner origins with AllowCredentials and still no wildcard.
- Verification: `dotnet format --verify-no-changes` clean; build 0 warnings; full suite **214 green** (37 integration incl. 10 auth/protection flows). `scripts/verify_starter.py` fixed this round to exclude generated/vendor directories (node_modules/.next/bin/obj/coverage/dist/build artifacts — ~31.5k files skipped).
- Migration/config/operations impact: new optional config section `Cors:AllowedOrigins`; reverse-proxy deployments must forward the correct Host and terminate TLS (SecurePolicy.Always).
- Residual risk: rate limiting on auth endpoints lands with M10-003 hardening; payment-gateway callback signature verification arrives with M06-004 (paths pre-exempted).
- Next unblocked tasks: M02-005 (SSO v1 scope decision doc), M01-007 (visual harness), M10-004 (runbooks).

