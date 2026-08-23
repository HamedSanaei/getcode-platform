# Module boundaries and ownership

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
