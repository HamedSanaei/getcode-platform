# ADR-014: Durable identifiers and database naming/migration policy

- Status: Accepted
- Date: 2026-08-24
- Decided by: M00-006
- Related: ADR-003 (PostgreSQL source of truth), ADR-007 (ledger idempotency), docs/architecture/DATABASE.md

## Context

The platform stores financial and fulfillment records whose identifiers must remain stable
for decades of audit, survive provider changes, and keep PostgreSQL index performance
predictable. The starter left the UUID/ULID decision open ("decided in M00") and had no
schema naming or migration-execution rules, which would let every future module invent its
own conventions.

## Decision

1. **UUIDv7** (`Guid.CreateVersion7()` from the .NET BCL) generated application-side is the
   canonical internal identifier for all durable records. Rationale: time-ordered values
   avoid the random-insert index fragmentation of UUIDv4; no extra package or database
   extension is required; the type is native `uuid` in PostgreSQL. ULIDs were rejected as
   redundant once UUIDv7 is available in the BCL.
2. **snake_case relational naming** (tables pluralised; `pk_`/`ux_`/`fk_`/`ix_` constraint
   prefixes) enforced centrally by a model convention so no migration can drift. Explicit
   per-entity configuration overrides the convention when necessary.
3. **Expand → migrate → contract** is the mandatory pattern for online schema evolution;
   destructive DDL additionally requires a restore-tested backup and release-gate review.
4. **Migrations are explicit operational events**: applications never auto-migrate;
   deployment runs an idempotent migration bundle under an advisory lock with a verified
   backup beforehand.

## Consequences

- Every aggregate must assign its identifier at creation (no database-generated GUIDs);
  this keeps identity valid before persistence and simplifies idempotent command handling.
- Schema reviews read generated SQL, not C# migration code; `SchemaShapeTests` pins the
  naming contract executably.
- Future modules get consistent naming for free but must route any exception through an
  explicit EF configuration plus a note in the entity configuration file.
