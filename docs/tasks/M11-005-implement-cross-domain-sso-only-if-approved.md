# M11-005: Implement cross-domain SSO only if approved

- Status: **TODO**
- Milestone: **M11**
- Priority: **P2**
- Depends on: M02-005, M10-003

## Goal

Implement cross-domain SSO only if approved.

## Acceptance criteria

- [ ] Central SSO follows approved standards-based design/threat model.
- [ ] Redirect/state/nonce/token rotation/logout are tested across both hosts.
- [ ] No cross-root-domain cookie workaround is used.

## Required verification

- [ ] two-domain browser security E2E

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
