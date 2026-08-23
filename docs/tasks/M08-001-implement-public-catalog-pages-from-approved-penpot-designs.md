# M08-001: Implement public catalog pages from approved Penpot designs

- Status: **TODO**
- Milestone: **M08**
- Priority: **P0**
- Depends on: M03-004, M01-007

## Goal

Implement public catalog pages from approved Penpot designs.

## Penpot implementation reference

Use the six `Public / *` boards on `GetCode · 04 Public Site`. Board IDs, likely routes and required states are recorded in `design/handoff/PENPOT_PAGE_MAP.md`. Do not start production UI work until product approval is recorded for these boards.

## Acceptance criteria

- [ ] Country/service/product browse/search is responsive/RTL/accessibility compliant.
- [ ] Pages use server API contracts and approved design tokens/components.
- [ ] Canonical metadata works on both hosts.

## Required verification

- [ ] component tests
- [ ] visual regression
- [ ] SEO metadata tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
