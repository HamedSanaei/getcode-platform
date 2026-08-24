# M04-005: Implement provider routing policy v1

- Status: **DONE**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-003, M04-004

## Goal

Implement provider routing policy v1.

## Acceptance criteria

- [x] Routing is an isolated policy with deterministic inputs and decision reason (`ProviderRoutingPolicy`, pure static, ordinal tie-breaks).
- [x] Price/availability/health are plain candidate facts; zero provider-name branching anywhere in business code.
-[x] Decision emits safe structured telemetry. (meter `GetCode.ProviderRouting` counter by reason token; reasons are stable ASCII).

## Required verification

-[x] routing unit tests
-[x] tie/failure tests (price ties broken deterministically; empty/all-unavailable inputs)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Providers/ProviderRoutingPolicy.cs`: v1 rules — exclude unavailable + unreachable (failure streak >= 3, aligned with M04-003 health semantics), lowest price wins, ties break by provider key ordinal; decision carries reason tokens: no-candidates / all-unavailable-or-unhealthy / only-viable-candidate / selected-lowest-price / selected-tie-broken-by-key.
- Telemetry: decisions counted on meter `GetCode.ProviderRouting` with reason attribute; no raw provider payloads.
- Orders integration arrives with M06 order flow — the policy is a pure function they call with candidate facts gathered from M04-004 offers + M04-003 health; nothing in this task touches controllers/orders yet (keeps this slice isolated and testable).
- Tests increased: backend 289 (+7).