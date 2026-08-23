# M04-007: Add second provider to prove abstraction

- Status: **TODO**
- Milestone: **M04**
- Priority: **P1**
- Depends on: M04-006

## Goal

Add second provider to prove abstraction.

## Acceptance criteria

- [ ] Second adapter passes same contract suite.
- [ ] No Order/Wallet/Catalog business code changes are required solely because provider was added.
- [ ] Router can choose/fail over between adapters using provider registry.

## Required verification

- [ ] common contract suite on both adapters
- [ ] architecture review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
