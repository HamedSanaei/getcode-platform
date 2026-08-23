# M09-001: Implement admin shell and permission-aware navigation

- Status: **TODO**
- Milestone: **M09**
- Priority: **P0**
- Depends on: M02-004, M01-007

## Goal

Implement admin shell and permission-aware navigation.

## Penpot implementation reference

Map to the six `Admin / *` boards on `GetCode · 09 Admin`, including provider operations, canonical mapping, pricing, order/refund support and mobile manual review. Exact IDs and state requirements are in `design/handoff/PENPOT_PAGE_MAP.md`.

## Acceptance criteria

- [ ] Admin UI is server-authorized; hidden navigation is not treated as authorization.
- [ ] Penpot admin patterns cover tables/filters/detail/audit/manual-review states.
- [ ] Dangerous actions require explicit confirmation/reason where appropriate.

## Required verification

- [ ] permission E2E
- [ ] visual/accessibility tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
