# M01-004: Implement Penpot-to-code design token bridge

- Status: **DONE**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-002, M00-001

## Goal

Implement Penpot-to-code design token bridge.

## Penpot implementation reference

- Canonical file and token-set names are recorded in `design/penpot/README.md` and `design/handoff/PENPOT_PAGE_MAP.md`.
- Initial bridge source is named Penpot version `GetCode Design System v1.1 — live HTML validation`, file revision `104`; record both in generated snapshot metadata.
- Source sets are `GetCode/Core`, `GetCode/Brand/GetCode` and `GetCode/Brand/PlusPremium`.
- The bridge must preserve semantic token names and generate/select a host brand without forking components.

## Acceptance criteria

- [x] Approved token snapshot is stored/versioned in design/tokens.
- [x] Next.js CSS/theme values are generated or deterministically mapped from tokens.
- [x] CI detects invalid token schema/drift according to documented policy.
- [x] Snapshot metadata records the canonical Penpot file ID and source revision used for generation.

## Required verification

- [x] token validation test
- [x] frontend build

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `design/tokens/getcode-tokens.v1.1.rev104.json`: versioned token snapshot exported live from the canonical Penpot file via the plugin API — metadata records Penpot file id `c269caa0-e456-818c-8008-85a77340be64`, named version `GetCode Design System v1.1 - live HTML validation`, revision `104`, design-system version 1.1.0, per-set counts; sets carry raw token name/type/value for Core (53), Brand/GetCode (7), Brand/PlusPremium (7).
  - `design/tokens/getcode-tokens.schema.json`: structural schema (allowed set names/types, name pattern, hex color shape, required metadata).
  - `frontend/scripts/tokens.mjs`: deterministic bridge with `generate` and `check` modes — validates schema (unique names, whitelisted types, hex colors, px/weight/opacity/font-family shapes, meta count parity) then renders CSS custom properties (`--gc-*`): Core under `:root`, GetCode brand as default on `:root,[data-brand="getcode"]`, PlusPremium overrides on `[data-brand="pluspremium"]` (no component forking).
  - `frontend/src/styles/tokens.css`: generated output (committed so drift is diffable).
  - `frontend/src/app/globals.css`: placeholder token block removed; styles now consume generated tokens (`--gc-color-surface-canvas`, brand hero gradient, ink text scale, radius/border tokens, Vazirmatn for `lang=fa`).
  - `frontend/package.json` scripts `tokens:generate` / `tokens:check`; `.github/workflows/ci.yml` frontend job runs `npm run tokens:check` before lint/typecheck/build.
- Decisions/assumptions:
  - Snapshot stores each set's own declared values (raw), not Penpot's resolved values — resolved values of the inactive PlusPremium set leak the active GetCode brand through shared names, which would misbrand the snapshot.
  - Brand selection is a `data-brand` attribute contract; components never fork per host.
  - Drift policy: any change to snapshot or generated CSS without regeneration fails CI; hand edits to tokens.css always fail.
- Verification commands: `node scripts/tokens.mjs check` green (schema + no drift); negative-path tamper test fails the gate correctly; `npm run lint`, `npm run typecheck`, `npm run build` all green.
- Migration/config/operations impact: none backend-side; CI frontend job gained one step.
- Residual risk: snapshot export is manual (plugin API pull) — re-export procedure is regenerate-check-commit until an automated Penpot API sync exists; font files (Inter/Vazirmatn) are not yet self-hosted (M01-005 concern).
- Next unblocked tasks: M01-005 (shared Next.js UI primitives) is unblocked by M01-003+M01-004; M01-006 (host-aware shell) follows M01-004.
