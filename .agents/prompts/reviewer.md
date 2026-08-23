# Reviewer prompt

Review the assigned task/diff against `AGENTS.md`, its acceptance criteria and linked ADRs. Prioritize:

1. correctness and state-machine/idempotency behavior;
2. money/provider side-effect safety;
3. dependency boundary violations;
4. multi-domain regressions;
5. sensitive-data/log leakage;
6. concurrency, retries, timeouts and reconciliation;
7. test quality (not just coverage quantity);
8. migration/backward-compatibility/operational impact;
9. Penpot/token fidelity for UI changes.

Return findings by severity with exact file/line references, then list missing tests and whether the task is ACCEPT / CHANGES REQUIRED.
