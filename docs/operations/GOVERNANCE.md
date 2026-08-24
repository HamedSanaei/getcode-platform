# Repository governance

How work moves through this repository: agents execute roadmap tasks, reviewers gate them,
CI enforces mechanically, and GitHub metadata mirrors `docs/roadmap/`.

## Agent execution and review loop

1. An implementing agent follows `.agents/prompts/task-executor.md` against exactly one
   task in `docs/tasks/`, per root `AGENTS.md`.
2. The agent leaves the handoff section (files changed, decisions, verification commands,
   operational impact, residual risk, next unblocked tasks) inside the task file.
3. A reviewer applies `.agents/prompts/reviewers.md` priorities — correctness/state
   machines first, then money/provider safety, boundaries, multi-domain, log leakage,
   concurrency/reconciliation, test quality, migration impact, Penpot fidelity.
4. Findings are recorded by severity with file/line references; verdict is
   ACCEPT / CHANGES REQUIRED. A worked example (the M00-002 review) lives at the bottom of
   this file.
5. Task status flips to DONE only after gates pass and the review is ACCEPT.

## Required checks / branch protection

`main` must require passing status checks before merge. With the current workflows:

| Check | Workflow | Gate |
|---|---|---|
| `backend` | CI | locked-mode restore, format, build, vulnerable-package audit, all tests |
| `frontend` | CI | npm ci audit gate, lint, typecheck, production build |
| `Analyze (csharp)` / `Analyze (javascript-typescript)` | CodeQL | static security analysis |
| `build (api/worker/web)` | Container build | images build on every PR |

Recommended repository settings (owner action, one time):

- Require pull requests before merging into `main` (0 approvals acceptable while solo;
  keep the required checks).
- Require branches to be up to date (`main`) before merge.
- Require linear history (no merge commits needed by policy; no force pushes ever).
- Restrict force pushes/deletes on `main` and release tags.

These settings cannot be applied from inside the repository; they are recorded here as the
standing governance requirement so every agent can assert what "cannot be bypassed" means.

## GitHub milestones/issues bootstrap

```bash
python scripts/github/bootstrap_github.py --repo HamedSanaei/getcode-platform --dry-run
python scripts/github/bootstrap_github.py --repo HamedSanaei/getcode-platform
```

- Dry-run prints the plan (12 milestones, 69 tasks) and touches nothing.
- The live run is idempotent: labels use `--force`, milestones/issues are deduplicated by
  exact title, so re-running never creates duplicates.
- Requires authenticated `gh` CLI (`gh auth login`) with write access to the repo.

## Worked example: reviewer record for M00-002

Applied the nine reviewer priorities to commit `68f0b37`
(`test(arch): complete M00-002 IL-based layer boundary enforcement`):

1. Correctness/state machines — n/a (test-only change). PASS.
2. Money/provider side effects — none touched. PASS.
3. Dependency boundaries — change itself adds IL-level enforcement; verified it fails on a
   deliberate violation (probe fixture, exit 1) before being removed. PASS.
4. Multi-domain — untouched. PASS.
5. Sensitive data/logs — no logging changes. PASS.
6. Concurrency/retries/timeouts — n/a. PASS.
7. Test quality — six NetArchTest policies map one-to-one onto BOUNDARIES.md rules;
   reflection-based originals retained as defense in depth. PASS.
8. Migration/backward compatibility — none; new dev dependency pinned centrally with
   lockfile updated. PASS.
9. Penpot fidelity — n/a. PASS.

Verdict: **ACCEPT** (no findings).

## Worked example: executor handoff for M00-009 (this task)

This file is itself the deliverable of M00-009's first acceptance criterion: the
executor → reviewer workflow was exercised end-to-end on the M00-002 sample, the
bootstrap dry-run was executed successfully (no duplicates, plan matches
`docs/roadmap/TASK_INDEX.md`), and required checks are documented above.
