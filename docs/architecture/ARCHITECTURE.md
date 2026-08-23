# GetCode software architecture

## 1. Architectural objective

GetCode starts as a virtual-number commerce and fulfillment product but must not encode “virtual number” as the entire commerce platform. The architecture separates commerce, fulfillment and suppliers so future digital fulfillment types can be introduced without rewriting money/order foundations.

## 2. Style

**DDD-oriented Modular Monolith + Clean Architecture + Ports & Adapters.**

A modular monolith is intentionally preferred over microservices at this stage. It gives one deployable transactional boundary while preserving explicit module ownership. Microservice extraction is an operational decision for measured scale/ownership needs, not an up-front topology goal.

## 3. Runtime topology

```text
                         ┌─────────────────────────┐
Independent domain ─────>│                         │
vnumber.pluspremium.ir ─>│ Edge / TLS / host ACL  │
                         └────────────┬────────────┘
                                      │
                         ┌────────────▼────────────┐
                         │ Next.js presentation     │
                         │ host aware / SEO / UX    │
                         └────────────┬────────────┘
                                      │ same-origin /api/*
                         ┌────────────▼────────────┐
                         │ ASP.NET Core API         │
                         └────────────┬────────────┘
                                      │
              ┌───────────────────────┼────────────────────────┐
              │                       │                        │
      ┌───────▼────────┐      ┌──────▼───────┐       ┌────────▼────────┐
      │ PostgreSQL      │      │ Redis        │       │ Worker           │
      │ durable truth   │      │ ephemeral    │       │ outbox/jobs      │
      └────────────────┘      └──────────────┘       └────────┬────────┘
                                                              │
                                                     provider/payment ports
                                                              │
                                             ┌────────────────┼──────────────┐
                                             ▼                ▼              ▼
                                         Provider A       Provider B      Provider C
```

## 4. Backend dependency rule

```text
GetCode.Domain
      ▲
      │
GetCode.Application
      ▲
      ├───────────────┐
      │               │
Persistence     Infrastructure
      ▲               ▲
      └───────┬───────┘
              │
          API / Worker
```

`Contracts` contains public transport records and does not define Domain behavior.

### Domain

Framework-free business invariants, aggregates, value objects and domain events.

### Application

Use cases, ports, authorization policies, orchestration that does not require infrastructure details.

### Persistence

EF Core/PostgreSQL mappings, durable repositories, outbox/inbox/idempotency persistence.

### Infrastructure

Provider/payment/notification/cache/messaging adapters, clock, logging and external transports.

### API / Worker

Composition roots. API owns HTTP concerns; Worker owns durable asynchronous execution. They do not contain domain rules.

## 5. Modules / bounded capabilities

Initial capability boundaries:

- Identity
- SiteHosts
- Catalog
- Providers
- Pricing
- Orders
- Fulfillment
- Activations
- Wallet
- Payments
- Refunds
- Promotions
- Notifications
- Audit
- Support

These are module boundaries, not necessarily separate assemblies today. If a boundary becomes difficult to enforce, add architecture tests or extract an assembly through an ADR rather than creating informal coupling.

## 6. Commerce / fulfillment / supplier separation

```text
Commerce Core                  Fulfillment                 Suppliers
-------------                  -----------                 ---------
Catalog                        VirtualNumber               Provider adapters
Pricing                        FutureFulfillmentX          health/capabilities
Orders                         FutureFulfillmentY          normalized errors
Wallet
Payments
Refunds
```

An Order says what the customer purchased. Fulfillment says how it is delivered. Providers are suppliers used by a fulfillment strategy.

## 7. Provider anti-corruption layer

Provider-specific country IDs, service strings, response models and statuses remain inside the provider adapter. Core code sees GetCode canonical keys and normalized errors only.

Provider routing is policy-driven. Price is one signal; availability, latency, success rate, cancellation behavior and provider health can become routing signals after reliable metrics exist.

Failover is permitted only when the previous attempt can be classified safely (definitely not reserved, safely cancellable, or reconciled). Ambiguous provider outcomes must enter reconciliation/manual-review logic instead of blind retry that can double-purchase.

## 8. Durable workflows

Long operations never keep browser HTTP requests open waiting for an SMS. API records intent/state; Worker performs durable work with leases, retries and reconciliation.

Critical side effects use:

- idempotency keys;
- explicit state machines;
- Transactional Outbox;
- bounded retry with jitter;
- timeout budgets;
- reconciliation of ambiguous outcomes;
- audit events.

## 9. Financial model

Wallet is ledger-based. Balance is derived/maintained from immutable ledger entries under transactional invariants. Every charge/refund/adjustment has a reason/reference/idempotency identity and an auditable actor/source.

Never represent correctness as only `user.Balance -= amount`.

## 10. Multi-domain architecture

Both public hosts are entry points into the same platform/data. Host-specific behavior is isolated in Site Context:

- public base URL;
- brand key/theme tokens;
- canonical URL policy;
- safe redirect/return URLs;
- host validation.

Cookies cannot be shared across unrelated registrable domains. If cross-domain single sign-on becomes required, implement a central standards-based SSO flow; do not attempt cross-root-domain cookie sharing.

## 11. Frontend architecture

Next.js owns presentation, routing, SSR/SEO and browser interactions. It calls ASP.NET through same-origin `/api/*`. It must not independently calculate authoritative price, wallet balance, order state transitions, provider routing or payment success.

Feature folders mirror user capabilities; shared components mirror the Penpot Design System.

## 12. Observability

Logs are structured JSON Lines with stable event names/context and UTC timestamps. Files roll daily and completed days are gzip archived under a year/month/service directory. Logs include correlation/trace/request/order/payment/provider-operation identifiers when relevant, but not secrets/OTP/raw SMS content.

OpenTelemetry-compatible tracing/metrics is a planned cross-cutting layer. Structured file logs remain a durable local operational fallback.

## 13. Evolution rule

Prefer an ADR before changing any locked cross-cutting decision. A new package/service is not architecture; prove why it solves a measured requirement and how it affects failure modes and operations.
