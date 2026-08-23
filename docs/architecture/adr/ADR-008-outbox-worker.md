# ADR-008: Transactional Outbox and durable Worker

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Commit business state and outbox intent atomically; Worker dispatches/processes asynchronously with leases/retries/reconciliation. Do not hold HTTP requests open for SMS.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
