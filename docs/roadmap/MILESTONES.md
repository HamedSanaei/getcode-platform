# GetCode milestones

Tasks are intentionally dependency-driven. Do not start a later task only because its milestone looks interesting; check dependencies in `TASK_INDEX.md`.

## M00 — Engineering Foundation

Lock versions, build/test/CI, persistence baseline, security and observability contracts.

## M01 — Penpot Design System & Web Foundation

Design UX in Penpot, token bridge, shared components, host-aware shell and visual test harness.

## M02 — Identity & Multi-Domain Sessions

Identity, permissions, secure host-scoped sessions, CSRF/CORS and multi-domain URL/SEO behavior.

## M03 — Catalog & Canonical Provider Model

Canonical countries/services/products/SKUs and provider mappings independent from vendor IDs.

## M04 — Provider Integration & Routing

Provider contract suite, first adapters, health, quote normalization, routing, failover and reconciliation.

## M05 — Pricing, Quotes & Wallet Ledger

Authoritative pricing, quote snapshots, ledger wallet and duplicate-safe financial operations.

## M06 — Orders & Payments

Order state machine, checkout, payment adapter/callback verification, idempotency and outbox.

## M07 — Fulfillment & Activations

Durable worker orchestration for reserve/poll/cancel/expire/refund/reconciliation.

## M08 — Customer Experience

Approved public catalog, checkout, dashboard, activation flow, wallet/order UX and SEO.

## M09 — Admin & Operations

Operational admin, provider health, manual review, audits, pricing/mapping controls and safe support tools.

## M10 — Production Hardening & Release

E2E/load/security/backup/restore/observability/release readiness.

## M11 — Scale & Product Expansion

Measured scaling: broker if justified, multi-worker coordination, new fulfillment types/reseller API/optional SSO.
