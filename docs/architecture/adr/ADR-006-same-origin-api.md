# ADR-006: Same-origin /api path

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Browser calls `/api/*`; edge routes that path to ASP.NET. This simplifies host-scoped sessions/CSRF/CORS while keeping backend independently deployable.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
