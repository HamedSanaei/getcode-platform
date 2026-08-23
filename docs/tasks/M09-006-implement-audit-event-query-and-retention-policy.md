# M09-006: Implement audit event query and retention policy

- Status: **TODO**
- Milestone: **M09**
- Priority: **P1**
- Depends on: M09-005

## Goal

Implement audit event query and retention policy.

## Acceptance criteria

- [ ] Audit records are distinct from debug logs and tamper-resistant within application permissions.
- [ ] Query access is restricted and sensitive payloads minimized.
- [ ] Retention/export policy is documented.

## Required verification

- [ ] audit authorization/integrity tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
