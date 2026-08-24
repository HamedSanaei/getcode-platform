# GetCode implementation status

Last scaffold update: **2026-08-24**

## Current phase

`M00 — Engineering Foundation: COMPLETE` → entering `M01/M02/M03` (design-gated + engineering tracks in parallel)

All nine M00 tasks are DONE and verified (toolchain baselines, IL-enforced boundaries, local PostgreSQL/Redis, hardened CI with deterministic lockfiles, Testcontainers integration infrastructure, UUIDv7/migration policy ADR-014, structured logging + redaction + archive verification, tracing/metrics foundation, governance gate). See `docs/roadmap/TASK_INDEX.md` for per-task handoffs.

The repository no longer contains only an architecture starter: persistence migrations, integration-test infrastructure, enforced observability policies and deterministic CI gates exist and run green locally (52 backend tests). No real provider, payment gateway or wallet mutation is implemented yet — that work starts at M03/M05/M06.

Design work was executed ahead of milestone order by explicit product-owner request. The Penpot design system and page models are implemented, but production UI code remains intentionally unimplemented and all engineering dependencies still apply.

## Penpot design status

- Canonical Penpot file and page IDs are recorded in `design/penpot/README.md` and `design/handoff/PENPOT_PAGE_MAP.md`.
- Eleven GetCode-owned Penpot pages cover foundations, reusable components, patterns, public site, auth/checkout, customer dashboard, activation/OTP, content/support, admin and responsive/edge states.
- GetCode and PlusPremium are separate brand token sets over one shared component model.
- Product-owner design approval and an evidence-backed live-site parity pass are still pending; implementation tasks must not treat the current design as approved until that review is recorded.

## Ready to start

- `M02-001` (identity) and `M03-001` (canonical catalog) are unblocked by the completed M00 foundation.
- `M01-004` (design token bridge), `M01-005` (UI primitives), `M01-006` (host-aware shell) are unblocked engineering tasks.
- `M01-001..003` remain IN_PROGRESS pending the product-owner design approval evidence gate (live-reference parity pack or recorded side-by-side approval).

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
- The product owner confirmed that `numberland.ir` loads in their visible browser on 2026-08-24. A second audit attempt located that exact open tab (matching title and URL), but the browser safety layer denied automated page reads; the independent read-only web fetch also returned a non-retryable error. This is an automation-access limitation, not a claim that the site is down.
- The current Penpot file is an engineering-complete GetCode design system derived from the preserved public Numberland reference, but **pixel parity with the live site is not verified**. Closing M01 design approval requires either a desktop/mobile reference evidence pack for all public routes or a recorded product-owner side-by-side approval.
