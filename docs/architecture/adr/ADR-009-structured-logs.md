# ADR-009: Structured daily logs and month-deletable archives

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Write JSONL with daily rolling; gzip completed days under `YYYY/MM/service`. Manual deletion of the month folder is a supported retention operation.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
