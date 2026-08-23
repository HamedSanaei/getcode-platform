# ADR-002: Monorepo

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Keep backend, frontend, design handoff, infrastructure, docs and agent instructions in one repository so contract changes and CI gates are atomic.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
