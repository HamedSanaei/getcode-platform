# ADR-015: Cross-domain SSO is out of v1 scope

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode serves two independent root domains (`getcode` primary host and
`vnumber.pluspremium.ir`) from one codebase and one identity store
(ADR-005). Users may hold sessions on both hosts at once.

**This ADR records the current, accepted v1 architecture — not an open TODO.**
The operative v1 design is: shared identity with per-host sessions and no
seamless SSO. It is implemented and enforced today by the M02-002 session
stack (`__Host-` cookie scoping plus server-side SiteMismatch rejection) and
verified by its integration tests.

## Decision

**v1 ships without seamless cross-domain SSO.** Each configured host maintains
its own host-scoped session (M02-002) backed by the shared identity store:

- One account, many sessions: signing in on either host authenticates the same
  user record; there is no account duplication by construction.
- Sessions are independent: logging out on one host does not affect the other;
  each cookie lives only on its own host (`__Host-` prefix contract).
- Cross-site token replay is refused server-side (SiteMismatch), so a leaked
  session from one host cannot be used on the other.

If product later requires seamless SSO, the approved design must be an
OIDC/OAuth-style central redirect/token-exchange flow with a full threat model
(redirect-uri allow-listing per site via Site Context, PKCE, state/nonce,
token audience binding) reviewed under a superseding ADR before implementation
begins in M11-005. Until then no SSO code, endpoints or shared-cookie tricks
may ship.

## UX contract while deferred

- After registering/signing in on one host, visiting the other host presents
  normal sign-in UI; where helpful, copy explains: *"Your account works on both
  GetCode sites — sign in again here to link this device."*
- Wallet balance, orders and identity data are visibly identical on both hosts
  (same account), which sets the expectation that only the *session* is
  per-host, never the data.
- No UI may imply that signing in on one host grants an authenticated state on
  the other.

## Consequences

- Implementations and reviews must preserve this decision; a conflicting
  change requires a superseding ADR plus migration/operational consequences.
- Seamless SSO remains explicitly deferred to M11-005 and may only proceed
  after product approval AND a superseding ADR with a full OIDC/OAuth
  threat-model review. No SSO code, endpoints or shared-cookie mechanisms may
  exist before that gate.
- The M02-005 task file retains the product-ratification note as process
  hygiene; it does not weaken the accepted status of this architecture.
