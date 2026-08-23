# M09-004: Implement pricing and promotion administration

- Status: **TODO**
- Milestone: **M09**
- Priority: **P1**
- Depends on: M05-001, M09-001

## Goal

Implement pricing and promotion administration.

## Acceptance criteria

- [ ] Pricing changes are validated/versioned/audited and can be previewed.
- [ ] Rules have effective-time semantics if required; existing orders remain unchanged.
- [ ] Permission boundaries separate view/manage.

## Required verification

- [ ] pricing admin integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
