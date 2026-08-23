# ADR-004: Provider anti-corruption layer

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Own canonical country/service/product/provider-operation contracts. Vendor DTOs/IDs/status strings stay inside Infrastructure adapters.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
