# M04-007: Add second provider to prove abstraction

- Status: **DONE**
- Milestone: **M04**
- Priority: **P1**
- Depends on: M04-006

## Goal

Add second provider to prove abstraction.

## Acceptance criteria

-[x] Second adapter passes same contract suite. (SecondVendorVirtualNumberProvider: 9/9 shared VirtualNumberProviderContractTests)
-[x] No Order/Wallet/Catalog business code changes were required — adapter + DI registration only; canonical port untouched.
-[x] Router resolves adapters via `IVirtualNumberProviderRegistry` by canonical key; candidate facts flow into the M04-005 policy with zero provider-name branching (test-pinned).

## Required verification

-[x] common contract suite on both adapters (five-sim 32 total contract tests incl. failure matrix; second-vendor 9)
-[x] architecture review (dependency-direction tests green; vendor wire models internal to Providers/SecondVendor; separation tests still green)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- New adapter `Infrastructure/Providers/SecondVendor/SecondVendorVirtualNumberProvider.cs`: deliberately different wire contract (string order ids, `state` strings, POST-style resources) mapped onto the SAME canonical port. Transport faults on reserve map to AmbiguousOutcome per M04-006 semantics; vendor error envelope maps to definitive refusals.
- IMPORTANT truthfulness note: "second-vendor" is a protocol-shaped abstraction proof, NOT a decided production vendor. Live use requires a product decision + credentials; it is opt-in via config (`SecondVendor:Enabled`, default off).
- Registry: `Application/Providers/IVirtualNumberProviderRegistry` + Infrastructure impl over all registered adapters (key-ordinal order). Routing/failover layers consume candidates from the registry - no business code references concrete adapters.
- DI: opt-in typed client like FiveSim; registry singleton collects IEnumerable<IVirtualNumberProvider>.
- Tests increased: backend 305 (+11: 9 contract-suite on second vendor, 2 registry/routing tests).