# M00-009: Agent handoff and repository governance gate

- Status: **TODO**
- Milestone: **M00**
- Priority: **P1**
- Depends on: M00-004

## Goal

Agent handoff and repository governance gate.

## Acceptance criteria

- [ ] Root AGENTS/task/reviewer workflow is exercised on one sample PR/task.
- [ ] Branch protection/required checks are documented.
- [ ] GitHub milestones/issues can be bootstrapped from roadmap data without duplicates.

## Required verification

- [ ] GitHub bootstrap dry-run
- [ ] agent reviewer checklist

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
