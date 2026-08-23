# Agent workspace

These files are model-agnostic. The supervisor assigns a single roadmap task to an implementation agent and a different review pass whenever possible.

Recommended flow:

1. Supervisor selects an unblocked task from `docs/roadmap/TASK_INDEX.md`.
2. Implementer reads root `AGENTS.md`, task file and linked ADRs.
3. Implementer changes code/tests/docs and records verification.
4. Reviewer runs `.agents/prompts/reviewer.md` against the diff.
5. Supervisor accepts only if acceptance criteria and quality gates are satisfied.

Do not ask an agent to “build the whole milestone” in one uncontrolled prompt.
