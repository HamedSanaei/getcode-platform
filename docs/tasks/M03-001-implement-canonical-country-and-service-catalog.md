# M03-001: Implement canonical Country and Service catalog

- Status: **TODO**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement canonical Country and Service catalog.

## Acceptance criteria

- [ ] Country/service identities are GetCode-owned stable keys.
- [ ] Localization/display metadata is separated from provider IDs.
- [ ] Enable/disable/order changes are auditable/admin-ready.

## Required verification

- [ ] domain tests
- [ ] persistence integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
