# M05-005: Implement exchange-rate source abstraction if required

- Status: **DONE**
- Milestone: **M05**
- Priority: **P2**
- Depends on: M05-001

## Goal

Implement exchange-rate source abstraction if required.

## Acceptance criteria

- [x] Product decision determines supported currencies/provider cost currencies. (DECISION RECORDED below — the "if required" gate resolved to NOT REQUIRED)
- [x] If conversion is required, rates are timestamped, cached safely and snapshotted into quotes. (N/A — no conversion path exists; see decision)
- [x] External rate source cannot mutate historical order values. (structurally guaranteed: orders store immutable money snapshots in a single currency; no external rate feed is wired anywhere in the codebase)

## Required verification

- [x] rate stale/failure tests (N/A — no rate source exists; re-open this task with the timestamped-cache design if cross-currency checkout ships)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24) — Decision: not required

- PRODUCT DECISION: GetCode prices each product directly in every supported customer currency via per-currency versioned rule sets (`PricingEngine.DefaultRuleSet`: RUB margin25+2fee min10; USD 20%+0.30 min0.50). Quotes, orders, payments and refunds are all single-currency by construction — there is NO runtime FX conversion between customer currency and provider cost currency.
- Provider cost currencies are absorbed into the per-currency rule sets at pricing-administration time, not converted at request time. This keeps historical order values immune to any external rate feed by construction (AC 3 satisfied structurally).
- Consequence: an exchange-rate source abstraction would be dead code today; building it would violate AGENTS.md ("no infrastructure for later without approved ADR"). Task resolved as NOT REQUIRED.
- RE-OPEN TRIGGER: if cross-currency checkout or dynamic provider-cost passthrough is ever added, reopen this task first; the design must then be timestamped rates, safely cached, snapshotted into quotes, never mutating persisted orders.
- Tests unchanged: backend 392.
