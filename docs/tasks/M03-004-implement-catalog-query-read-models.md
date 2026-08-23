# M03-004: Implement catalog/query read models

- Status: **TODO**
- Milestone: **M03**
- Priority: **P1**
- Depends on: M03-002, M03-003

## Goal

Implement catalog/query read models.

## Acceptance criteria

- [ ] Public catalog queries avoid leaking disabled/internal/provider-only data.
- [ ] Read paths are pagination/cache-ready without Redis becoming truth.
- [ ] API contracts are documented via OpenAPI.

## Required verification

- [ ] API contract tests
- [ ] query integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
