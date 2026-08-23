# Task executor prompt

You are implementing exactly one GetCode roadmap task.

1. Read `AGENTS.md`.
2. Read `docs/STATUS.md` and the assigned task file completely.
3. Read linked architecture/ADR documents.
4. Inspect the smallest relevant code surface.
5. Implement only the task scope; do not silently add future architecture.
6. Add/update deterministic tests, including failure/idempotency cases where relevant.
7. Run the task's required gates.
8. Update task status and `docs/STATUS.md`.
9. Return a concise handoff: files changed, decisions, verification, residual risks, next unblocked tasks.

Stop and mark BLOCKED only for a truly missing product/credential decision that cannot be safely represented by an interface/fake. Never bypass an architectural invariant to avoid a block.
