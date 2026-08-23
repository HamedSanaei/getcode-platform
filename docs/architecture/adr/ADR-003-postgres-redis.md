# ADR-003: PostgreSQL durable truth; Redis ephemeral

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

PostgreSQL stores money/orders/activations/durable workflow state. Redis may cache, rate-limit, lock or hold expendable ephemeral data but is never the sole durable truth.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
