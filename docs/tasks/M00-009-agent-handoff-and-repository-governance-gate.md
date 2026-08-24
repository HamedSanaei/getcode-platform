# M00-009: Agent handoff and repository governance gate

- Status: **DONE**
- Milestone: **M00**
- Priority: **P1**
- Depends on: M00-004

## Goal

Agent handoff and repository governance gate.

## Acceptance criteria

- [x] Root AGENTS/task/reviewer workflow is exercised on one sample PR/task.
- [x] Branch protection/required checks are documented.
- [x] GitHub milestones/issues can be bootstrapped from roadmap data without duplicates.

## Required verification

- [x] GitHub bootstrap dry-run
- [x] agent reviewer checklist

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `docs/operations/GOVERNANCE.md` (new — execution/review loop, required-checks table mirroring current workflow job names, branch-protection recipe, bootstrap usage, worked reviewer record for M00-002 and executor handoff for this task), `docs/CODE_MAP.md` (pointer), task/index status flips.
- Verification: `bootstrap_github.py --dry-run` prints 12 milestones + 69 tasks exactly matching `TASK_INDEX.md`; dedup logic reviewed in source (labels via `gh label create --force`, milestones/issues deduplicated by exact title). The nine-priority reviewer prompt was exercised as a filled record against completed commit `68f0b37` with verdict ACCEPT. Live bootstrap requires an authenticated `gh` CLI and is recorded in GOVERNANCE.md as a one-time owner action alongside branch protection settings.
- Migration/config/operations impact: documentation only; no code or CI changes.
- Residual risk: actual GitHub settings (branch protection, live issue creation) require repository-admin credentials unavailable in this environment; documented as standing requirements, not blockers for engineering work.
- Milestone outcome: with this task, **M00 is complete** (M00-001…M00-009 DONE). Next milestone work starts at M01-004/M01-005/M01-006 (design approval gate keeps M01-001..003 IN_PROGRESS pending product-owner evidence).
