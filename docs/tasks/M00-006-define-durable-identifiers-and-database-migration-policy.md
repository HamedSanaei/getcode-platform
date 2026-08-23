# M00-006: Define durable identifiers and database migration policy

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-005

## Goal

Define durable identifiers and database migration policy.

## Acceptance criteria

- [ ] Choose/document UUID/ULID identifier policy and DB naming conventions.
- [ ] Create initial reviewed migration for foundational tables only.
- [ ] Document expand/contract and production migration execution rules.

## Required verification

- [ ] migration applies on empty DB
- [ ] schema snapshot review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
