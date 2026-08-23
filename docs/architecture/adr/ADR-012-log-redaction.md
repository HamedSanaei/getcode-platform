# ADR-012: Observability data minimization

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Forbid secrets, tokens, OTPs/raw SMS and raw vendor payload dumps. Log normalized safe context and masked identifiers.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
