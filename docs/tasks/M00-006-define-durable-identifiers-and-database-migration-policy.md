# M00-006: Define durable identifiers and database migration policy

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-005

## Goal

Define durable identifiers and database migration policy.

## Acceptance criteria

- [x] Choose/document UUID/ULID identifier policy and DB naming conventions.
- [x] Create initial reviewed migration for foundational tables only.
- [x] Document expand/contract and production migration execution rules.

## Required verification

- [x] migration applies on empty DB
- [x] schema snapshot review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Decisions (recorded in ADR-014 + `docs/architecture/DATABASE.md`): UUIDv7 via `Guid.CreateVersion7()` generated application-side is the canonical durable identifier (time-ordered → healthy indexes; BCL-native, no ULID package); snake_case relational naming (`pk_`/`ux_`/`fk_`/`ix_`) enforced centrally by a model convention with explicit-config-wins semantics; expand→migrate→contract for online schema change; production migrations run as idempotent bundles under advisory lock with verified backup — never auto-migrate at startup.
- Files changed: `GetCode.Persistence/Conventions/NamingConventions.cs` (new), `GetCodeDbContext.cs` (applies conventions), regenerated `Migrations/*InitialCreate*` + snapshot (foundational outbox table only), `docs/operations/sql/0001_baseline.sql` (reviewed idempotent script artifact), `SchemaShapeTests.cs` (executable schema snapshot review against real PostgreSQL), docs listed above.
- Verification: migration applies on an empty per-run container (integration fixture does exactly this every run) — 7/7 integration tests green including the new schema-shape assertions (`information_schema`/`pg_constraint`/`pg_indexes` checked against the documented names). Full gates: format ✓, build 0 warn/0 err ✓, 23/23 tests ✓.
- Migration/config/operations impact: baseline SQL artifact committed for ops review; deployment pipeline must adopt bundle+advisory-lock execution before first production release (tracked under M10).
- Assumption: pluralised table names follow the existing `outbox_messages` precedent; no product owner objection recorded.
- Next unblocked tasks: M02-001, M03-001 (identity and catalog chains open up), M00-007.
