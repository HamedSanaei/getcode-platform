# M07-005: Implement reconciliation and Manual Review cases

- Status: **DONE**
- Milestone: **M07**
- Priority: **P0**
- Depends on: M07-004

## Goal

Implement reconciliation and Manual Review cases.

## Acceptance criteria

-[x] System identifies stuck/ambiguous records BY RULE. (`ReconciliationRuleEngine` pure rules over `ReconciliationFacts`: unknown payment verification, dead-lettered fulfillment jobs, reservations expired without message; healthy systems yield zero findings)
-[x] Manual Review stores evidence/reason/status without exposing secrets. (evidence is built from token grammar only — test pins that raw bodies/payloads never appear)
[x] Admin action resolves through application commands, not direct DB edits. (`ManualReviewService.ResolveAsync` records actor/timestamp/note; double-resolve is rejected and the first resolution stands — audit-pinned)

## Required verification

[x] reconciliation rule tests
[x] manual resolution audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Reconciliation/ManualReview.cs`: ReviewCase record + IManualReviewStore port + ReconciliationRuleEngine (pure) + ManualReviewService (SweepAsync/ListOpenAsync/ResolveAsync). One open case per subject+reason; resolution is an audited command with actor, timestamp and note; re-resolution rejected.
- Residual: Persistence EF implementation of IManualReviewStore (review_cases table + migration) and the periodic sweep host wiring land as infrastructure follow-ups; the contracts are pinned by unit tests.
- Tests increased: backend 392 (+5 rule/dedupe/redaction/audit tests).