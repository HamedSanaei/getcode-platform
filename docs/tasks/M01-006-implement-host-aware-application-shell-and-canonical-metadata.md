# M01-006: Implement host-aware application shell and canonical metadata

- Status: **TODO**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-004

## Goal

Implement host-aware application shell and canonical metadata.

## Acceptance criteria

- [ ] Both configured hosts render from one codebase with correct brand token context.
- [ ] Unknown-host behavior is explicit.
- [ ] Canonical metadata points to configured canonical host without creating arbitrary open redirects.

## Required verification

- [ ] host resolution unit tests
- [ ] metadata tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
