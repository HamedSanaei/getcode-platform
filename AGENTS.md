# AGENTS.md — GetCode engineering contract

This file is normative for every coding agent and human contributor.

## Mission

Build GetCode incrementally without eroding the agreed architecture. Prefer small, testable changes with explicit contracts over clever shortcuts.

## Non-negotiable architecture

1. Backend is a **DDD-oriented modular monolith** using Clean Architecture dependency direction.
2. `GetCode.Domain` has no dependency on ASP.NET Core, EF Core, Redis, provider SDKs, payment SDKs or infrastructure frameworks.
3. `GetCode.Application` may depend on Domain abstractions but never on Persistence, Infrastructure, API or Worker.
4. Provider-specific DTOs, IDs, status strings and SDK types never escape `Infrastructure/Providers/<ProviderName>`.
5. Canonical concepts (country, service, product/SKU, activation state) belong to our model; provider values are mappings.
6. PostgreSQL is the source of truth. Redis is never the only store for money, orders, activations or durable workflow state.
7. Money mutations are ledger-based and idempotent. No feature may directly “just decrement balance”.
8. External side effects must be retry-safe. Payment callbacks, order creation, provider reservations, refunds and outbox dispatch require idempotency/reconciliation strategy.
9. Use Transactional Outbox before introducing a broker. RabbitMQ is not a default dependency; see ADR-011.
10. Multi-domain support is mandatory: independent GetCode host + `vnumber.pluspremium.ir`. Do not hard-code a public host in business logic.
11. UI is designed in Penpot first. React/Next.js implementation must map approved Penpot components/tokens rather than inventing a parallel design language.
12. Logs are structured, redacted and correlation-friendly. Never log secrets, bearer tokens, cookies, provider API keys, OTPs or raw SMS bodies by default.

## Task execution protocol

Before coding:

1. Open `docs/STATUS.md`.
2. Open the assigned `docs/tasks/<TASK-ID>-*.md` file.
3. Read every ADR linked by the task.
4. Inspect only the bounded code needed for the task.
5. State assumptions in the PR/task handoff; do not silently invent product rules.

During coding:

- Keep changes inside task scope.
- Add/update tests in the same change.
- Preserve dependency direction.
- Do not add a package when a BCL/framework capability is sufficient.
- Do not add infrastructure “for later” without an approved ADR.
- Do not edit generated migrations or lockfiles by hand.
- Never put secrets in source, test snapshots, logs or examples.

Before completion:

- Run the relevant format/lint/build/test gates.
- Add an architecture test when introducing a new architectural invariant.
- Update `docs/STATUS.md` and the task file status.
- Update ADR/architecture docs if a decision changed.
- Record migrations, environment variables and operational impact.
- Leave the repository in a state the next agent can understand without chat history.

## Definition of done

A task is not done merely because the happy path works. It is done when acceptance criteria, tests, error paths, observability, documentation and security implications are covered.

## Forbidden shortcuts

- provider `if/else` logic scattered across business code;
- static/global mutable service locator;
- database access from controllers or React components;
- provider API calls while holding a database transaction open;
- fire-and-forget money or fulfillment operations;
- `AllowAnyOrigin` with credentials;
- sharing cookies across unrelated registrable domains;
- putting domain logic into Next.js server actions;
- logging whole request/response bodies from provider/payment APIs;
- disabling tests or warning gates to make CI green.
