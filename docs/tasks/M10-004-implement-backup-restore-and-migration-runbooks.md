# M10-004: Implement backup, restore and migration runbooks

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement backup, restore and migration runbooks.

## Acceptance criteria

- [ ] Automated PostgreSQL backup/PITR strategy is configured for target environment.
- [ ] Restore drill succeeds and is timed/documented.
- [ ] Deployment migration and rollback/forward-fix procedure is tested.

## Required verification

- [ ] restore drill evidence
- [ ] migration rehearsal

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
