# M09-003: Implement catalog/provider mapping management

- Status: **TODO**
- Milestone: **M09**
- Priority: **P0**
- Depends on: M03-003, M09-001

## Goal

Implement catalog/provider mapping management.

## Acceptance criteria

- [ ] Admin can manage mappings with validation/preview and audit trail.
- [ ] Invalid/duplicate mapping cannot corrupt canonical catalog.
- [ ] Changes do not rewrite historical order snapshots.

## Required verification

- [ ] mapping mutation tests
- [ ] audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
