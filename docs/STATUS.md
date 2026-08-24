# GetCode implementation status

Last scaffold update: **2026-08-24**

## Current phase

`M00 — Engineering Foundation: COMPLETE` → entering `M01/M02/M03` (design-gated + engineering tracks in parallel)

All nine M00 tasks are DONE and verified (toolchain baselines, IL-enforced boundaries, local PostgreSQL/Redis, hardened CI with deterministic lockfiles, Testcontainers integration infrastructure, UUIDv7/migration policy ADR-014, structured logging + redaction + archive verification, tracing/metrics foundation, governance gate). See `docs/roadmap/TASK_INDEX.md` for per-task handoffs.

The repository now exposes its first public API surface: paged catalog reads (`/api/catalog/countries|services|offers`) documented via OpenAPI, alongside identity/authentication with durable lockout and audit, the canonical Country/Service catalog with outbox-audited admin mutations, the Product/SKU model (provider-free identity), the provider registry with validated mappings and a reusable behavioral contract suite, deny-by-default role/permission authorization with audited privilege changes, a wallet + immutable ledger with concurrency-safe idempotent money mutations, integration-test infrastructure, enforced observability policies and deterministic CI gates — 196 backend tests green. No real provider integration or payment gateway is implemented yet — that work starts at M04/M06 once provider credentials and gateway selection are available.

Design work was executed ahead of milestone order by explicit product-owner request. The Penpot design system and page models are implemented, but production UI code remains intentionally unimplemented and all engineering dependencies still apply.

## Penpot design status

- Canonical Penpot file and page IDs are recorded in `design/penpot/README.md` and `design/handoff/PENPOT_PAGE_MAP.md`.
- Eleven GetCode-owned Penpot pages cover foundations, reusable components, patterns, public site, auth/checkout, customer dashboard, activation/OTP, content/support, admin and responsive/edge states.
- GetCode and PlusPremium are separate brand token sets over one shared component model.
- M01-001, M01-002 and M01-003 are DONE against their documented acceptance criteria. A direct-curl live HTML audit inventoried 163 routes across 20 families and validated the objective structure against Penpot; owner review remains only for visual differences not settled by HTML/CSS plus the preserved snapshot.

## Ready to start

- `M03-001..004` are DONE (milestone M03 complete): canonical catalog, Product/SKU model, provider registry + validated mappings, and public paged read models (`/api/catalog/*`) with OpenAPI-documented contracts.
- `M04-001` is DONE: reusable provider behavioral contract suite (search/reserve/status/cancel/error/timeout/cancellation + leakage guards); the fake adapter is now deterministic and failure-injectable.
- `M02-004` is DONE: permission-catalog-based roles with deny-by-default server-side resolution, user-role assignments, and outbox-audited privilege changes (migration `AddAuthorization`). HTTP enforcement wiring lands with M02-002/M02-003 sessions.
- `M04-002` (first real provider adapter) is BLOCKED on the product decision: which real provider to onboard first, plus API credentials via secret manager. Everything downstream that only needs the fake/contract layer proceeds.
- `M05-003` is DONE: wallet + immutable ledger — minor-unit money, xmin optimistic concurrency with backoff retry, database-unique idempotency keys, compensating refunds/adjustments, 8-way concurrent-debit race proven safe (migration `AddWallets`). `M05-004` (idempotent debit/credit/refund primitives) is next in dependency order and fully unblocked; `M01-004` (design token bridge) is also eligible.
- `M01-004` is ready: its M01-002 and M00-001 dependencies are DONE. `M01-005` and `M01-006` follow the token bridge.
- `M02-002` (host-scoped session/token strategy) waits on `M01-006`.

## Locked architecture decisions

- Modular Monolith + Clean Architecture + DDD-oriented + Ports & Adapters.
- ASP.NET Core 10 backend; Next.js 16.x frontend.
- PostgreSQL durable source of truth; Redis ephemeral only.
- Multiple external virtual-number providers hidden behind GetCode-owned ports and canonical mappings.
- Transactional Outbox before a message broker.
- One shared application/data model served from independent GetCode domain and `vnumber.pluspremium.ir`.
- Same-origin browser API path `/api/*` through the edge; no business logic in Next.js.
- Ledger-based wallet and idempotent financial mutations.
- Penpot is the UI source of truth.
- Structured JSONL logging, daily rolling, gzip archive under `logs/YYYY/MM/<service>/`; manual month-folder deletion supported.
- Sensitive data/OTP/token/raw SMS logging forbidden by default.

## Known placeholders

- Independent production domain is unknown (`getcode.example` placeholder).
- Real provider list and provider credentials are unknown.
- Payment gateway(s) are unknown.
- Authentication product policy and whether cross-root-domain SSO is required for v1 are not finalized.
- `numberland.ir` is reachable. The 2026-08-24 direct-curl audit downloaded live HTML, inventoried 163 internal routes across 20 families and fetched 17 representative pages with HTTP 200; evidence is in `design/handoff/NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md`.
- Exact pixel parity remains an owner-review question only where HTML/CSS and the preserved snapshot are insufficient. It is not an acceptance blocker for the completed sitemap, foundations or reusable-component tasks.
