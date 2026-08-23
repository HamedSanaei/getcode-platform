# M02-003: Implement CSRF, CORS and trusted redirect policy

- Status: **TODO**
- Milestone: **M02**
- Priority: **P0**
- Depends on: M02-002

## Goal

Implement CSRF, CORS and trusted redirect policy.

## Acceptance criteria

- [ ] State-changing browser requests have a CSRF strategy compatible with chosen auth.
- [ ] Credentialed CORS is allow-listed; same-origin remains default.
- [ ] Return/redirect URLs are selected from Site Context allow-list.

## Required verification

- [ ] CSRF negative tests
- [ ] origin/redirect abuse tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
