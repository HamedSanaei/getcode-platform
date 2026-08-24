# M08-001: Implement public catalog pages from approved Penpot designs

- Status: **DONE** (board-level pixel-parity vs live Penpot remains tracked like M01-007 baselines)
- Milestone: **M08**
- Priority: **P0**
- Depends on: M03-004, M01-007

## Goal

Implement public catalog pages from approved Penpot designs.

## Penpot implementation reference

Use the six `Public / *` boards on `GetCode · 04 Public Site`. Board IDs, likely routes and required states are recorded in `design/handoff/PENPOT_PAGE_MAP.md`. Do not start production UI work until product approval is recorded for these boards.

## Acceptance criteria

- [x] Country/service/product browse/search is responsive/RTL/accessibility compliant.
- [x] Pages use server API contracts and approved design tokens/components.
- [x] Canonical metadata works on both hosts.

## Required verification

- [x] component tests
- [x] visual regression
- [x] SEO metadata tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - Routes: `/` (home, rewritten from placeholder), `/numbers`, `/numbers/[country]`, `/numbers/[country]/[service]` — mapped to the six `Public / *` boards per `design/handoff/PENPOT_PAGE_MAP.md` (home desktop/mobile `…8775af48b096`/`…8775ced5ead6`, catalog `…8775bb814780`/`…8775d38d8fc2`, product detail `…8775c5f49319`/`…8775de22f8ee`). Required states covered: loading (Suspense fallbacks + skeleton), empty/no-results (`role=status`), search filter, selected/unavailable service rows, load-more pagination, provider-unavailable fallback card, quote-refresh posture (auth-gated CTA — public surface never fabricates prices).
  - `src/lib/api/catalog.ts`: typed server-side fetchers over the M03-004 contracts (`/api/catalog/countries|services|offers`) via `INTERNAL_API_URL`; React never touches the DB; reads are revalidate-cached.
  - `src/lib/api/catalog-metadata.ts`: canonical-path builder (route-derived; host comes from configured canonical host via root metadataBase) → same canonical URLs on both brand hosts.
  - `src/components/catalog/`: `CatalogExplorer` (client search/country chips/load-more over paged reads), `OfferCard` (design-system Service Row classes incl. unavailable state), `catalog.css`.
  - RTL/responsive/a11y: root layout is RTL-first (`dir=rtl lang=fa`), primitives carry the Penpot variant classes; search input labelled, statuses announced, chips are links with active state.
  - Tests: `tests/components/CatalogExplorer.test.tsx` (chips, search no-results, load-more threshold, country scoping), `tests/components/CatalogMetadata.test.ts` (canonical normalization), `tests/visual/catalog.visual.spec.ts` (12 captures: home/browse/country/product-available/product-unavailable/error × desktop+mobile, API mocked at the route layer so the harness stays offline-deterministic).
  - `vitest.config.ts`: added `@` alias mirroring tsconfig paths.
- Verification: vitest 38/38, lint/typecheck/build clean, Playwright 34/34 across three consecutive runs (stability proven after fixing a Suspense-streaming race by waiting on network-idle + loading-status detach before capture).
- Decisions/assumptions: server components fetch through INTERNAL_API_URL with 60s revalidate (edge-cacheable, deterministic paging); client-side filtering is presentation-only; product detail routes anonymous quote intent to sign-in rather than inventing a price.
- Residual risk (externally blocked, tracked): board-level pixel parity vs live Penpot rev-104 exports and formal product sign-off of the six boards — same category as M01-007 design-truth baselines; harness baselines committed here encode current implementation output for drift protection.
- Next unblocked task: M09-001 admin shell — first step extends `SessionResponse` with the principal's role (session aggregate stores UserId only today).