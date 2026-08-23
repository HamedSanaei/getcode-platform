# M00-002: Enforce clean dependency and module boundaries

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Enforce clean dependency and module boundaries.

## Acceptance criteria

- [ ] Architecture tests enforce Domain/Application forbidden references.
- [ ] Document module ownership and cross-module write rule.
- [ ] CI fails on a demonstrated forbidden reference test fixture or equivalent verification.

## Required verification

- [ ] architecture tests
- [ ] full backend build

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
