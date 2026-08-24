# Database migration runbook (M10-004)

Migrations are a controlled deployment step — never concurrent app startup.

## Policy

1. **Forward-only.** Down-migrations are never run against production.
   Rollback means redeploying the previous application version, which must work
   against the current schema.
2. **Expand-only changes** make rule 1 possible: new columns/tables are added
   nullable or with defaults; old code paths keep working; destructive changes
   (drop/rename/not-null tighten) ship in a later release after the new code is
   live everywhere. Migrations are reviewed against this rule.
3. Generated migration files and `packages.lock.json` are never hand-edited.
4. Every deployment that carries migrations rehearses them first (below).

## Pre-deployment rehearsal (required)

```sh
# Regenerate the idempotent full-schema script from migrations:
dotnet ef migrations script --idempotent \
  --project backend/src/GetCode.Persistence \
  --startup-project backend/src/GetCode.Api \
  --output docs/operations/sql/0001_baseline.sql

# Rehearse on a scratch database:
sh scripts/db/migrate-rehearsal.sh rehearsal_scratch
```

The rehearsal proves, on an isolated database:

- forward apply of the complete migration script succeeds;
- core tables exist (`users`, `sessions`, `wallets`, `wallet_entries`, `roles`,
  `outbox_messages`, `providers`);
- a simulated forward-fix applied on top does not break re-runs;
- re-applying the idempotent script is safe (no-op for existing objects).

### Rehearsal log

| Date | Baseline | Result | Evidence |
|---|---|---|---|
| 2026-08-24 | migrations through `AddSessions` (rev 2026-08-24) | PASS — all 7 core probes ok; forward-fix + idempotent re-run clean | agent-run drill in compose Postgres 18 |

## Deployment procedure

1. Put the site in maintenance mode if the release contains behavior-visible
   changes (read-only banner is enough for most releases).
2. Take a fresh logical backup (`BACKUP_RESTORE.md`) and confirm PITR archiving
   is current (`pg_stat_archiver.failed_count = 0`).
3. Apply migrations: `dotnet ef database update` (or the generated idempotent
   script via psql) **before** starting the new application version.
4. Start/deploy the new version; watch structured logs for migration-related
   errors and run the health endpoint + one authenticated smoke flow.
5. Exit maintenance mode.

## Rollback / forward-fix

- **Application defect, schema compatible:** redeploy previous image. The
  expand-only policy guarantees it runs against the current schema.
- **Application defect, schema incompatible (should not happen):** restore the
  pre-deployment backup per `BACKUP_RESTORE.md`, then redeploy the previous
  version. This is why step 2 exists.
- **Forward fix preferred:** when data written by the new version must be kept,
  ship a small corrective deploy instead of rolling back. Documented decision
  goes in the incident record.

## Compatibility contract for reviewers

A PR carrying a migration must state: which expansion it performs, whether the
previous release works against it, and when the eventual contract-breaking part
(drop/tighten) will ship.
