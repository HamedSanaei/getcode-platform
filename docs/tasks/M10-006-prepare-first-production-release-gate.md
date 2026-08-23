# M10-006: Prepare first production release gate

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M10-003, M10-004, M10-005

## Goal

Prepare first production release gate.

## Acceptance criteria

- [ ] Release checklist includes DNS/TLS/hosts/canonical SEO/secrets/backups/migrations/rollback.
- [ ] Containers are immutable/tagged and deployment records exact versions.
- [ ] All required CI/E2E/security gates are green before tag.

## Required verification

- [ ] release rehearsal
- [ ] smoke tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
