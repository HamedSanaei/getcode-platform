# M03-002: Implement Product/SKU model for virtual-number offerings

- Status: **DONE**
- Milestone: **M03**
- Priority: **P0**
- Depends on: M03-001

## Goal

Implement Product/SKU model for virtual-number offerings.

## Acceptance criteria

- [x] SKU expresses canonical service/country/product type and commercial availability.
- [x] Provider selection is not stored as the customer product identity.
- [x] Model leaves room for future fulfillment types.

## Required verification

- [x] domain invariant tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `GetCode.Domain/Catalog/ProductSku.cs` + `ProductSkuEvents.cs` (aggregate keyed by canonical (country, service, product type) triple; `ProductType` enum starts with Activation/Rental and is append-only for future fulfillment kinds; `StableKey` derived display string); `GetCode.Application/Catalog/ProductSkuServices.cs` (`IProductSkuRepository` port, `ProductSkuAdminService` resolving commands by canonical code/slug, outbox audit under `catalog.product_sku.*`, `ProductCatalogQueryService` composing localized names from catalog entries only); `GetCode.Persistence/Catalog/ProductSkuConfiguration.cs` (unique index on identity triple, FKs to countries/services) + repository + DI + migration `AddProductSkus`.
- Decisions/assumptions:
  - No pricing on SKUs yet — money model arrives with M05 (quotes/margins). Commercial state is a single audited `IsOffered` flag; entries start unoffered.
  - Provider selection is absent by construction; a reflection guard test asserts no provider/vendor concept ever appears on the aggregate surface.
  - Storefront listing composes country/service display names from enabled catalog entries; SKUs referencing disabled entries are excluded from listings even when offered.
- Verification commands: format verify clean; build 0 warnings/errors; full suite **143 tests green** (UnitTests 88 incl. SKU domain invariants + admin fakes, IntegrationTests 17 incl. SKU uniqueness roundtrip + composed localized queries).
- Migration/config/operations impact: expand-only `product_skus` table; no env changes.
- Residual risk: rental vs activation semantics are placeholders until fulfillment design lands (M06); per-SKU display overrides not implemented (composition from catalog names covers current needs).
- Next unblocked tasks: M03-003 (provider registry + canonical mappings), then M03-004 (catalog read models).
