# M05-001: Implement pricing rules and margin model

- Status: **DONE**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M03-002, M04-004

## Goal

Implement pricing rules and margin model.

## Acceptance criteria

-[x] Authoritative sell price is computed server-side from explicit rules (`PricingEngine` + versioned `PricingRule` records; client input never sets price).
-[x] Rounding/currency/margin boundaries are documented and tested. (ceiling-to-cent = never undercharge; half-cent rounds up; MinSellAmount floor after rounding; negative cost / unknown currency fail fast; per-currency rules, no silent defaults)
-[x] Historical order price does not change when a rule changes — computations are immutable snapshots stamping the rule Version at computation time (test: old snapshot stays 52.00/v3 while recompute yields 62.00/v4).

## Required verification

-[x] pricing boundary/property tests (+ determinism loop: sell >= cost always, <=2dp always)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Pricing/PricingEngine.cs`: PricingRule(Version, Currency, MarginPercent, FixedFeeAmount, MinSellAmount) + PriceComputation(Currency, CostAmount, SellAmount, RuleVersion, ComputedAtUtc). Formula: ceil-to-cent(cost x (1+margin%) + fixedFee), floored at min. Decimal-only math.
- Rule set wiring (config-driven per-currency) lands with M05-002 quote snapshots, which will persist PriceComputation immutably - that is where historical-price durability becomes structural rather than in-memory.
- Tests increased: backend 314 (+9).