# M03-003: Implement provider registry and canonical mappings

- Status: **TODO**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M03-001

## Goal

Implement provider registry and canonical mappings.

## Acceptance criteria

- [ ] Provider registry has stable provider keys/capability metadata.
- [ ] Country/service/product mapping belongs to provider capability, not Domain vendor fields.
- [ ] Mapping changes are validated and auditable.

## Required verification

- [ ] mapping tests
- [ ] persistence integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
