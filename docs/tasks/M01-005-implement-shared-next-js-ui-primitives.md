# M01-005: Implement shared Next.js UI primitives

- Status: **TODO**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-003, M01-004

## Goal

Implement shared Next.js UI primitives.

## Penpot implementation reference

Implement against named Penpot version `GetCode Design System v1.1 — live HTML validation`, especially `GetCode · 02 Components`; exact variant axes and board IDs are in `design/handoff/PENPOT_PAGE_MAP.md`. Production components must cite their Penpot asset and variant mapping and consume the M01-004 token bridge rather than copying Numberland CSS values directly.

## Acceptance criteria

- [ ] Shared components map to named Penpot components/variants.
- [ ] Keyboard/focus/RTL behavior meets handoff.
- [ ] No API/business rules are embedded in primitives.

## Required verification

- [ ] component interaction tests
- [ ] accessibility checks

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
