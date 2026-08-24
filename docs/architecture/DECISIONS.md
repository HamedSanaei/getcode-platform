# Architecture decision index

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Modular monolith + Clean Architecture + DDD-oriented boundaries | Accepted |
| ADR-002 | Monorepo for backend/frontend/design/infrastructure | Accepted |
| ADR-003 | PostgreSQL durable source of truth; Redis ephemeral | Accepted |
| ADR-004 | Provider anti-corruption layer and canonical model | Accepted |
| ADR-005 | One application served on independent domain + vnumber.pluspremium.ir | Accepted |
| ADR-006 | Same-origin browser `/api/*` edge routing | Accepted |
| ADR-007 | Ledger-based wallet + idempotent money mutations | Accepted |
| ADR-008 | Transactional Outbox + durable Worker workflows | Accepted |
| ADR-009 | Structured daily JSONL logs + monthly gzip archive tree | Accepted |
| ADR-010 | Penpot as UI source of truth | Accepted |
| ADR-011 | No message broker by default; introduce on measured need | Accepted |
| ADR-012 | Sensitive-data minimization/redaction in observability | Accepted |
| ADR-013 | Configurable canonical SEO host | Accepted |
| ADR-014 | UUIDv7 durable identifiers; snake_case naming; expand/contract migrations | Accepted |
| ADR-015 | Cross-domain SSO out of v1 scope; separate host sessions over shared identity | Accepted |
