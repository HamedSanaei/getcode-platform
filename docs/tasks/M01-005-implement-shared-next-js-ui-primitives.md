# M01-005: Implement shared Next.js UI primitives

- Status: **DONE**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-003, M01-004

## Goal

Implement shared Next.js UI primitives.

## Penpot implementation reference

Implement against named Penpot version `GetCode Design System v1.1 — live HTML validation`, especially `GetCode · 02 Components`; exact variant axes and board IDs are in `design/handoff/PENPOT_PAGE_MAP.md`. Production components must cite their Penpot asset and variant mapping and consume the M01-004 token bridge rather than copying Numberland CSS values directly.

## Acceptance criteria

- [x] Shared components map to named Penpot components/variants.
- [x] Keyboard/focus/RTL behavior meets handoff.
- [x] No API/business rules are embedded in primitives.

## Required verification

- [x] component interaction tests
- [x] accessibility checks

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `frontend/src/components/ui/`: seven primitives, one per Penpot variant group — `Button` (Style × State × Size), `TextField` (State × Type; label/error/hint wiring with `aria-invalid` + `aria-describedby`, visually-hidden label option), `Tabs` (WAI-ARIA tabs: roving tabindex, Arrow/Home/End navigation with RTL-mirrored arrow semantics), `Badge` (Tone), `ServiceRow` (available/unavailable states, presentational price/badge slots), `Alert` (Tone; danger/warning → `role="alert"`, success/info → `role="status"`), `SidebarItem` (`aria-current="page"`). Barrel `index.ts`; mapping table in the folder README.
  - `ui.css`: all styling consumes `--gc-*` token-bridge variables exclusively; logical CSS properties throughout (RTL-safe without direction branches).
  - Test toolchain added as devDependencies: vitest, @vitejs/plugin-react, jsdom, @testing-library/{react,user-event,jest-dom}, jest-axe (+types). `vitest.config.ts`, `tests/setup.ts` (jest-dom matchers + axe matcher registration + cleanup), and four suites in `tests/ui/`: Button interaction/disabled/axes, TextField label-error-required-disabled wiring, Tabs ARIA + roving tabindex + arrow keys incl. RTL mirror test, Badge/Alert/ServiceRow/SidebarItem semantics. Every primitive also passes an axe scan.
  - `package.json`: `test` script; `.github/workflows/ci.yml`: frontend job runs `npm test` after lint.
- Decisions/assumptions:
  - Variant axes follow the handoff contract in PENPOT_PAGE_MAP.md (Style/State/Size etc.). Exact enumerated values for Style/Tone were implemented from the token vocabulary (primary/accent/secondary/ghost; neutral/success/warning/danger/info/brand); pixel-level parity against the live boards must be confirmed by the M01-007 visual harness because the Penpot plugin connection dropped mid-task and could not be re-established from this session.
  - Primitives embed zero business rules: no fetches, no routing, no pricing logic; ServiceRow availability is expressed via `aria-disabled` + native disabled only.
- Verification commands: `npx vitest run` → 19/19 green (interaction + axe); `npm run tokens:check`, `npm run lint`, `npm run typecheck`, `npm run build` all green.
- Migration/config/operations impact: none backend-side; CI frontend job gained `npm test`.
- Residual risk: visual parity vs `GetCode · 02 Components` boards unverified until Penpot reconnect + M01-007 harness (documented above); Tabs RTL arrows verified via forced `direction: rtl` since jsdom does not inherit document direction into computed styles.
- Next unblocked tasks: M01-006 (host-aware application shell + canonical metadata) is fully unblocked and opens the M02-002 session chain.