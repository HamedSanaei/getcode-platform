# M10-003: Complete production security hardening

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M10-001, M02-003

## Goal

Complete production security hardening.

## Acceptance criteria

- [ ] Threat model covers auth, wallet/payment, provider abuse, webhook replay, admin and multi-domain sessions.
- [ ] Rate limits/WAF/headers/proxy trust/secret rotation are production configured.
- [ ] Dependency/code scans have no unresolved critical findings without explicit risk acceptance.

## Required verification

- [ ] security test report
- [ ] scanner gates

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
