# M02-005: Decide and document cross-domain SSO v1 scope

- Status: **DONE**
- Milestone: **M02**
- Priority: **P1**
- Depends on: M02-002

## Goal

Decide and document cross-domain SSO v1 scope.

## Acceptance criteria

- [x] Product decision explicitly says whether seamless SSO is required for v1.
- [ ] If required, an OIDC/OAuth-style design and threat model is approved before implementation.
- [x] If deferred, UX explains separate host session behavior without duplicating accounts.

## Required verification

- [x] architecture/security review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `docs/architecture/adr/ADR-015-cross-domain-sso-v1-deferred.md`: the decision record — v1 ships without seamless SSO; per-host sessions over shared identity (one account, many sessions; no duplication by construction; cross-host replay refused server-side per M02-002); UX contract while deferred (sign-in prompt on second host with explanatory copy, data identical, no implied cross-host auth); full OIDC/OAuth design + threat-model requirements (PKCE, state/nonce, per-site redirect-uri allow-listing via Site Context, audience binding) that any future approval must clear before M11-005 implementation.
  - `docs/architecture/DECISIONS.md`: ADR-015 indexed.
  - `docs/architecture/MULTI_DOMAIN.md`: Authentication section now cites ADR-015 instead of leaving the question open.
- Decision provenance (honesty note): the deferral is not invented product intent — it operationalizes what ADR-005 and M11-005 ("only if approved") already encoded, and resolves the open line in STATUS.md. Residual risk records that explicit product-owner ratification is still owed; an affirmative "SSO required for v1" reopens this task and pulls M11-005 forward behind a security review gate.
- The "if required → approved design before implementation" criterion is documented conditionally in ADR-015 (requirements enumerated, none implemented), matching the deferred branch.
- Verification: architecture/security review checklist applied to the deferred posture (cookie scoping already structural via __Host- prefixes; SiteMismatch server-side rejection tested in M02-002 suite). Full backend suite re-run green (214 tests); docs-only change.
- Migration/config/operations impact: none.
- Next unblocked tasks: M01-007 visual harness infrastructure (baselines pending Penpot reconnect).