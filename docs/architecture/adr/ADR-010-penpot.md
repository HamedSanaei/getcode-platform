# ADR-010: Penpot is UI source of truth

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Foundations/components/patterns/pages are designed and approved in Penpot before production Next.js UI implementation. Token and component mappings prevent design drift.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
