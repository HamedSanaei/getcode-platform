# Module boundaries and ownership

## Layer dependency contract (enforced by `GetCode.ArchitectureTests`)

```
Api / Worker (composition roots)
  └─> Application <── implemented by ──> Persistence, Infrastructure
         └─> Domain                          └─> Application + Domain
Contracts (standalone transport types)
```

- **Domain** may depend only on the BCL. Forbidden: other GetCode layers, ASP.NET Core,
  EF Core, Npgsql, Redis, Serilog/any logging framework, `System.Net.Http`.
- **Application** depends on Domain only. Forbidden: Persistence, Infrastructure, Api,
  Worker and all infrastructure frameworks.
- **Contracts** are self-contained transport DTOs; no GetCode dependencies.
- **Persistence** implements Application ports; may use EF Core/Npgsql only.
- **Infrastructure** implements Application ports; provider SDKs stay inside
  `Infrastructure/Providers/<ProviderName>` (anti-corruption layer).
- Api and Worker are composition roots and wire implementations into Application ports.

| Rule | Test |
|---|---|
| Domain BCL-only | `LayerDependencyRules.Domain_depends_only_on_the_BCL` |
| Application inward-only | `LayerDependencyRules.Application_does_not_reach_outer_layers_or_infrastructure_frameworks` |
| Contracts standalone | `LayerDependencyRules.Contracts_are_self_contained_transport_types` |
| Persistence allowed deps | `LayerDependencyRules.Persistence_implements_application_and_domain_only` |
| Infrastructure allowed deps | `LayerDependencyRules.Infrastructure_implements_application_and_domain_only` |
| Provider ACL containment | `LayerDependencyRules.Core_layers_never_reference_provider_adapter_namespaces` |

These rules are IL-based: referencing a forbidden namespace inside a method body fails the
build even when the project reference graph would allow it. Verified for M00-002 with a
deliberate `System.Net.Http` usage in Domain — the suite failed naming the offending type.

## Module ownership

| Capability | Owns | Must not own |
|---|---|---|
| Identity | user identity, credentials/session policy, roles/permissions | wallet/order logic |
| SiteHosts | host resolution, public URL policy, brand/canonical metadata | business pricing/provider selection |
| Catalog | canonical countries/services/product definitions/SKUs | provider raw IDs, live balances |
| Providers | provider capability/mapping/health abstractions | customer order lifecycle |
| Pricing | quote/margin/rate rules and immutable quote snapshots | wallet mutation |
| Orders | purchase intent and order state machine | provider HTTP calls |
| Fulfillment | delivery orchestration contract | direct payment verification |
| Activations | virtual-number activation lifecycle and messages metadata | provider SDK DTO leakage |
| Wallet | wallet accounts and ledger invariants | gateway protocol |
| Payments | payment intents/verification/callback semantics | product fulfillment |
| Refunds | refund policy/orchestration | arbitrary ledger mutation |
| Promotions | promotion eligibility/discount semantics | provider routing |
| Notifications | notification intent/templates/delivery adapters | order truth |
| Audit | immutable security/admin/business audit events | debug log storage |
| Support | support/manual-review workflows | bypassing domain invariants |

## Cross-module communication

Within the monolith, communicate through application contracts/domain events and explicit queries. Do not let one module reach into another module's EF entity/table and mutate it directly.

Cross-module reads can be optimized later with read models. Cross-module writes require an owned use case.
