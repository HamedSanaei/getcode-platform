# ADR-001: Modular monolith with Clean Architecture and DDD-oriented boundaries

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Use a single deployable backend initially, with dependency direction and capability boundaries enforced by tests. Microservices are deferred until operational/team scaling justifies extraction.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
