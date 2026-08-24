# M01-006: Implement host-aware application shell and canonical metadata

- Status: **DONE**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-004

## Goal

Implement host-aware application shell and canonical metadata.

## Penpot implementation reference

Implement the shell against `Header / GetCode / Navigation / Header Desktop`, `Bottom Nav / GetCode / Navigation / Bottom Mobile`, `Pattern · Authenticated App Shell` and the two-brand contract on `GetCode · 10 Responsive & States`. Exact page/board IDs and token-set names are recorded in `design/handoff/PENPOT_PAGE_MAP.md`. Host selection changes semantic brand tokens and canonical metadata, not component structure.

## Acceptance criteria

- [x] Both configured hosts render from one codebase with correct brand token context.
- [x] Unknown-host behavior is explicit.
- [x] Canonical metadata points to configured canonical host without creating arbitrary open redirects.

## Required verification

- [x] host resolution unit tests
- [x] metadata tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `frontend/src/lib/site/site-config.ts`: documented, tested host policy — two configured hosts (`GETCODE_PRIMARY_HOST`, `GETCODE_PLUSPREMIUM_HOST`), case/port normalization; unknown hosts fall back to primary config but carry `hostKnown: false`; canonical host comes only from `GETCODE_CANONICAL_HOST` (default: primary). `brandKey` aligned to the token bridge scopes (`getcode` | `pluspremium`).
  - `frontend/src/components/shell/AppShell.tsx` + `BottomNav.tsx` + `shell.css`: authenticated app-shell pattern per Penpot (`Header Desktop`, `Bottom Mobile`, responsive switch ≤48rem); identical markup both hosts — only the `data-brand` attribute differs. BottomNav marks the live route with `aria-current="page"`.
  - `frontend/src/app/layout.tsx`: wraps children in AppShell; emits `robots: { index:false, follow:false }` when `hostKnown` is false; `metadataBase` stays env-derived.
  - Tests: `tests/site/site-config.test.ts` (6 host-resolution cases), `tests/site/metadata.test.ts` (3 metadata/no-open-redirect/noindex cases), `tests/site/AppShell.test.tsx` (landmarks, active route, axe scan).
- Decisions/assumptions:
  - Unknown-host policy: serve primary config but exclude from indexes — mirrors/preview hosts cannot outrank canonical content; documented in site-config.ts header.
  - Both hosts render `lang="fa" dir="rtl"` (pre-existing choice kept); locale-per-host remains available via SiteConfig if product later differentiates.
  - Header/bottom-nav link targets (/orders, /wallet, /account) are placeholder routes until M02 sessions create real pages.
- Verification commands: full vitest suite 31/31 green (host resolution, metadata contract, shell interaction + axe); tokens/lint/typecheck/build green; production-server smoke test with forced Host headers confirmed: primary → `data-brand="getcode"` + canonical `https://<canonical>`; pluspremium → `data-brand="pluspremium"` + indexable; hostile mirror host → getcode fallback + `robots noindex, nofollow` + canonical still pointing at configured host.
- Migration/config/operations impact: none backend-side; deployment must set GETCODE_*_HOST env vars (already wired in CI build step).
- Residual risk: shell visual parity vs Penpot boards pending M01-007 harness + Penpot reconnect; navigation routes are placeholders.
- Next unblocked tasks: M01 milestone P0 chain complete → M02-002 (host-scoped session/token strategy) now unblocked by M01-006; M01-007 (visual regression harness) unblocked too.
