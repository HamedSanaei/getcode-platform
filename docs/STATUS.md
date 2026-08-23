# GetCode implementation status

Last scaffold update: **2026-08-24**

## Current phase

`M00 — Engineering Foundation`

The repository contains an architecture starter only. No real provider, payment gateway, wallet mutation, authentication or production UI is considered implemented.

Design work was executed ahead of milestone order by explicit product-owner request. The Penpot design system and page models are implemented, but production UI code remains intentionally unimplemented and all engineering dependencies still apply.

## Penpot design status

- Canonical Penpot file and page IDs are recorded in `design/penpot/README.md` and `design/handoff/PENPOT_PAGE_MAP.md`.
- Eleven GetCode-owned Penpot pages cover foundations, reusable components, patterns, public site, auth/checkout, customer dashboard, activation/OTP, content/support, admin and responsive/edge states.
- GetCode and PlusPremium are separate brand token sets over one shared component model.
- Product-owner design approval and a live-site parity pass are still pending; implementation tasks must not treat the current design as approved until that review is recorded.

## Ready to start

Start with `M00-001`, then follow dependencies in `docs/roadmap/TASK_INDEX.md`.

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
- Direct interactive inspection of `numberland.ir` was blocked by the browser safety layer during the design pass; a dated public snapshot is preserved in Penpot and live-site parity remains a review item.
