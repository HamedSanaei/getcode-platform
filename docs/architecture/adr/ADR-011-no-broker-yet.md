# ADR-011: No message broker by default

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Start with PostgreSQL Outbox + Worker. Add RabbitMQ or another broker only after measured throughput/latency/decoupling requirements justify its operational cost.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
