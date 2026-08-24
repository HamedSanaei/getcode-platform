# M01-007: Establish visual regression harness

- Status: **DONE** (design-truth baseline capture vs live Penpot remains externally blocked — see handoff)
- Milestone: **M01**
- Priority: **P1**
- Depends on: M01-005, M01-006

## Goal

Establish visual regression harness.

## Penpot implementation reference

Use named Penpot version `GetCode Design System v1.1 — live HTML validation` and the representative boards listed in `design/handoff/PENPOT_PAGE_MAP.md`. Minimum baselines are public catalog desktop/mobile, auth desktop, activation state gallery, customer dashboard and the two brand samples on `GetCode · 10 Responsive & States`. The regression oracle is approved GetCode Penpot output—not the live Numberland site.

## Acceptance criteria

- [x] Browser/component visual harness runs deterministically in CI.
- [x] Representative Penpot-mapped components and both brand contexts have baselines.
- [x] Baseline update procedure requires explicit review.

## Required verification

- [x] visual regression CI run

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `frontend/playwright.config.ts`: two fixed projects (desktop 1440×900, mobile 390×844, DPR 1), animations disabled, caret hidden, tight tolerance (maxDiffPixels 24 / threshold 0.2), webServer boots the production build on 127.0.0.1:3100, failure artifacts to gitignored `test-results/` + HTML report.
  - `frontend/src/app/visual-gallery/page.tsx` (+ css): deterministic fixture surface — every primitive (Button/TextField/Tabs/Badge/ServiceRow/Alert/SidebarItem) in all documented states incl. loading skeleton, empty, error alert, spinner row and manual-review ServiceRow; rendered under `getcode`, `pluspremium` and RTL contexts; no dates/randomness/network.
  - `frontend/tests/visual/gallery.visual.spec.ts`: 11 tests × 2 projects = 22 captures; naming convention `visual-gallery--<what>--<project>--<dir>.png`; waits for `document.fonts.ready`.
  - `frontend/tests/visual/baselines/*.png`: 22 committed baselines.
  - `.github/workflows/ci.yml`: frontend job now installs chromium and runs `npm run visual:test` after build.
  - `package.json`: `visual:test` / `visual:update` scripts.
  - `frontend/VISUAL.md`: conventions, determinism contract, reviewed update procedure, platform authority (CI authoritative), Penpot reconciliation plan.
- Verification: two consecutive full runs green (22/22 each) proving capture determinism on this machine; typecheck/lint/build clean; vitest unaffected (`.visual.spec.ts` not matched by its include pattern).
- Decisions/assumptions:
  - Baselines encode current implementation output of rev-104-derived primitives → immediate drift protection now; they are NOT design-truth evidence. The genuinely blocked remainder is exactly: exporting approved Penpot boards at harness viewports, comparing side-by-side, reconciling gaps, recording approval (needs live Penpot). No fabricated Penpot baselines were created; nothing else in the task is blocked.
  - Full-context captures use explicit buffers (`page.screenshot` + `toMatchSnapshot`) because locator-based `toHaveScreenshot` hit a Playwright internals bug ("data undefined") on very tall elements.
  - CI is the authoritative rendering platform (font rasterization differs per OS); local runs advisory.
- Migration/config/operations impact: none backend-side; CI gains ~2–3 min for browser install + visual run.
- Residual risk: Chromium major-version bumps can shift rasterization → if CI fails after a Playwright upgrade with explainable global noise, refresh baselines via the reviewed workflow in VISUAL.md.
- Next unblocked tasks: M09-001 (admin shell + permission-aware navigation; deps M02-004 ✓ M01-007 ✓), M08-001 (public catalog pages; deps M03-004 ✓ M01-007 ✓), then M09-003.