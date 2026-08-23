# Start here

This starter is designed to be copied into the empty repository:

`https://github.com/HamedSanaei/getcode-platform`

## 1. Put the starter into the repository

Extract the ZIP so `README.md`, `GetCode.sln`, `frontend/`, `backend/`, `docs/` and the other top-level folders are at repository root.

Then:

```bash
git add -A
git commit -m "chore: bootstrap GetCode platform architecture"
git push origin main
```

## 2. Do not jump directly into product features

Assign an agent **M00-001** first. The frontend dependency lock is intentionally deferred to this gate because a Next.js security release was announced for 2026-08-26; the agent must select the current patched supported 16.x patch and commit the lockfile before deployment.

## 3. Create GitHub milestones/issues (optional but recommended)

After installing/authenticating GitHub CLI:

```bash
python scripts/github/bootstrap_github.py --repo HamedSanaei/getcode-platform --dry-run
python scripts/github/bootstrap_github.py --repo HamedSanaei/getcode-platform
```

This creates the 12 milestones and 69 task issues from the repository roadmap and avoids duplicate exact-title issues on re-run.

## 4. Give agents repository context, not chat history

A task agent should receive only the task ID plus instructions to follow `AGENTS.md`. The repository itself contains architecture, ADRs, task dependencies, Penpot workflow and verification requirements.

Suggested first agent prompt:

> Implement M00-001 from `docs/tasks/`. Follow `AGENTS.md`, linked architecture decisions and the task acceptance criteria. Keep scope bounded, run all required checks, update task/status docs, and return a reviewer-ready handoff.
