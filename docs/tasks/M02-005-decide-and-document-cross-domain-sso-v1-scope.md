# M02-005: Decide and document cross-domain SSO v1 scope

- Status: **TODO**
- Milestone: **M02**
- Priority: **P1**
- Depends on: M02-002

## Goal

Decide and document cross-domain SSO v1 scope.

## Acceptance criteria

- [ ] Product decision explicitly says whether seamless SSO is required for v1.
- [ ] If required, an OIDC/OAuth-style design and threat model is approved before implementation.
- [ ] If deferred, UX explains separate host session behavior without duplicating accounts.

## Required verification

- [ ] architecture/security review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
