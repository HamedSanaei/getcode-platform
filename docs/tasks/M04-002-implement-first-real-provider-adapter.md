# M04-002: Implement first real provider adapter

- Status: **TODO**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-001

## Goal

Implement first real provider adapter.

## Acceptance criteria

- [ ] Provider HTTP client has explicit timeout/auth/user-agent and cancellation.
- [ ] Vendor DTOs and IDs are contained inside its Infrastructure folder.
- [ ] Adapter passes common contract tests using fake/stub HTTP, without spending real balance in CI.

## Required verification

- [ ] contract tests
- [ ] HTTP mapping tests
- [ ] redaction tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
