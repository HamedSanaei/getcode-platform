# ADR-005: Shared application on two domains

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Serve independent GetCode domain and vnumber.pluspremium.ir from one codebase/data model. Treat host branding/URL/SEO/session concerns as Site Context, not tenants.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
