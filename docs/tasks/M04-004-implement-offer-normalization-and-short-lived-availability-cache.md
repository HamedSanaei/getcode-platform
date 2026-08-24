# M04-004: Implement offer normalization and short-lived availability cache

- Status: **DONE**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-002, M03-004

## Goal

Implement offer normalization and short-lived availability cache.

## Acceptance criteria

- [x] Offers normalize provider cost/currency/availability with observed timestamp.
- [x] Cache expiry/staleness is explicit; purchases never consult the cache — reservations go live to the provider (structurally pinned by test).
- [x] Store loss degrades to the provider path rather than corrupting truth (cache is best-effort behind IAvailabilityCacheStore; in-memory now, Redis swappable later).

## Required verification

-[x] cache fallback tests (store throwing ⇒ live path still succeeds)
-[x] stale offer tests (expired entry refreshed; stale copy serves explicitly when provider faults)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Providers/OfferAvailabilityCache.cs`: NormalizedOfferSet (canonical offers + ObservedAtUtc/ExpiresAtUtc), IAvailabilityCacheStore + InMemoryAvailabilityCacheStore, ProviderOfferQueryService pipeline: fresh hit → serve; expired → live refresh; provider fault with cached copy → serve explicitly-stale set (stale-while-error); ANY store failure → transparent live-path degradation.
- Normalization boundary re-validates adapter output (blank keys / negative costs rejected, blank currency → "XXX", observation timestamps stamped uniformly) so nothing corrupts downstream pricing.
- Purchase safety: the service exposes no reserve/purchase member (test-pinned); reservations always hit the provider live. Invalidate() exists for future admin/quote flows.
- Redis note: a distributed store implements IAvailabilityCacheStore later; degradation semantics are already pinned at this layer per ADR-011 spirit (PostgreSQL/provider is truth).
- Tests increased: backend 282 (+6). Residual: distributed store implementation and public storefront wiring land with M05 quote work.