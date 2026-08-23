# M00-004: Harden CI quality gates

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001, M00-002

## Goal

Harden CI quality gates.

## Acceptance criteria

- [ ] Backend restore/format/build/test gates are deterministic.
- [ ] Frontend uses lockfile install, lint, typecheck and production build.
- [ ] CodeQL/container build workflows are validated; failed gates cannot be bypassed by task agents.

## Required verification

- [ ] GitHub Actions dry review
- [ ] local equivalent gates

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
