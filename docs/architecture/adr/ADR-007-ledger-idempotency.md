# ADR-007: Ledger-based wallet and idempotent money changes

- Status: Accepted
- Date: 2026-08-24

## Context

GetCode needs an explicit, agent-readable cross-cutting decision to prevent local implementation shortcuts from redefining platform architecture.

## Decision

Financial truth is expressed as immutable ledger entries/references and duplicate-safe commands/callbacks, not direct ad-hoc balance mutation.

## Consequences

- Implementations and reviews must preserve this decision.
- A conflicting change requires a superseding ADR with migration/operational consequences.
