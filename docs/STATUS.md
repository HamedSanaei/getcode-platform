# GetCode implementation status

Last scaffold update: **2026-08-24**

## Current phase

`M00 — Engineering Foundation: COMPLETE` → entering `M01/M02/M03` (design-gated + engineering tracks in parallel)

All nine M00 tasks are DONE and verified (toolchain baselines, IL-enforced boundaries, local PostgreSQL/Redis, hardened CI with deterministic lockfiles, Testcontainers integration infrastructure, UUIDv7/migration policy ADR-014, structured logging + redaction + archive verification, tracing/metrics foundation, governance gate). See `docs/roadmap/TASK_INDEX.md` for per-task handoffs.

The repository now exposes its first public API surface: paged catalog reads (`/api/catalog/countries|services|offers`) documented via OpenAPI, alongside identity/authentication with durable lockout and audit, the canonical Country/Service catalog with outbox-audited admin mutations, the Product/SKU model (provider-free identity), the provider registry with validated mappings and a reusable behavioral contract suite, deny-by-default role/permission authorization with audited privilege changes, a wallet + immutable ledger with payload-aware idempotent money mutations, integration-test infrastructure, enforced observability policies and deterministic CI gates — 198 backend tests green. No real provider integration or payment gateway is implemented yet — that work starts at M04/M06 once provider credentials and gateway selection are available.

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
- `M05-003` and `M05-004` are DONE: wallet + immutable ledger — minor-unit money, xmin optimistic concurrency with backoff retry, database-unique idempotency keys (migration `AddWallets` + `AddLedgerRequestHash`), compensating refunds/adjustments, 8-way concurrent-debit race proven safe; payload-aware idempotency (same key + same semantic request replays; conflicting payload rejected and audited via `wallet.idempotency_conflict`; crash/retry proven duplicate-free).
- `M01-004` is DONE: Penpot token snapshot (file `c269caa0...be64`, rev 104, v1.1 live-HTML validation) versioned under `design/tokens/` with a structural schema; deterministic generator emits brand-scoped CSS (`data-brand` contract, no component forking); CI runs a schema+drift gate before the frontend build. `M01-005`/`M01-006` follow it.
- `M01-005` is DONE: seven shared UI primitives (Button/TextField/Tabs/Badge/ServiceRow/Alert/SidebarItem) map 1:1 to the `GetCode · 02 Components` variant groups, consume only the token bridge, and carry vitest interaction tests + axe accessibility scans (19 tests; CI runs them). Visual pixel-parity vs live boards awaits Penpot reconnect + M01-007 harness.
- `M01-006` is DONE: host-aware app shell (Header Desktop + Bottom Mobile per Penpot) renders both hosts from one codebase via `data-brand`; unknown hosts fall back to primary but are explicitly noindex'd; canonical metadata is env-derived only — hostile Host headers cannot hijack it (proven by unit + live smoke tests). M02-002 session chain is now unblocked.
- `M02-002` is DONE: server-side sessions in Postgres (opaque 256-bit tokens, SHA-256 at rest), `__Host-`-prefixed per-site cookies (structurally cannot span the two unrelated root domains), server-side site re-validation on every request, 7-day absolute lifetime, rotation + single/all revocation, full HTTP test coverage on both hostnames incl. cross-site replay rejection.
- `M02-003` is DONE: CSRF double-submit (`__Host-xcsrf` cookie + `X-XSRF-TOKEN` header via `/api/auth/csrf`) plus Origin enforcement on all state-changing `/api/*` browser writes; credentialed CORS is allow-list-only with empty-config deny-by-default; trusted redirects resolve exclusively through the Site Context allow-list (foreign/scheme-relative/backslash targets collapse to the current site base).
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
