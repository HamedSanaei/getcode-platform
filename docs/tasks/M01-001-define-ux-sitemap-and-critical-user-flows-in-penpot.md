# M01-001: Define UX sitemap and critical user flows in Penpot

- Status: **IN_PROGRESS**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M00-009

## Goal

Define UX sitemap and critical user flows in Penpot.

## Acceptance criteria

- [x] Penpot file/workspace is recorded in design/penpot README.
- [x] Public browse -> quote -> checkout -> activation and customer dashboard flows are mapped.
- [x] Admin is a separate flow surface; RTL/mobile states are included.

## Required verification

- [ ] design review checklist

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Implemented in Penpot file `c269caa0-e456-818c-8008-85a77340be64`. The canonical page/board/flow map is `design/handoff/PENPOT_PAGE_MAP.md`. No migration, environment-variable or runtime impact. Residual: product-owner design review and live Numberland parity check are pending, so the task remains `IN_PROGRESS`.
