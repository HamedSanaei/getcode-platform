# Backup / restore runbook (M10-004)

PostgreSQL is the source of truth for all durable state. Redis is a cache/lease
layer only and is **never** treated as disaster-recovery truth: losing it costs
warm caches and in-flight lease timers, nothing else.

## Strategy

| Layer | Mechanism | Schedule | Retention |
|---|---|---|---|
| Logical safety net | `scripts/db/backup.sh` (`pg_dump` custom format, compressed, checksummed) | Nightly + before every migration window | 14 days default (tune per environment) |
| Point-in-time recovery | WAL archiving (`archive_command` → durable volume/object store) via `infrastructure/postgres/compose.pitr.yaml` pattern | Continuous (`archive_timeout=60s` bounds gap loss) | ≥ PITR window (7 days recommended) |
| Physical base backup | `pg_basebackup` from the archive-enabled instance | Before the nightly logical dump | Keep ≥ 2 to satisfy the PITR window |

- **RPO target:** ≤ 5 minutes (WAL segments ship at latest switch; `archive_timeout=60s` forces a segment at least once per minute of activity).
- **RTO target:** ≤ 1 hour for full database recovery on production-sized volumes; measured restore throughput from drills feeds this estimate.
- Secrets/config are stored in the secret manager, never inside backups. A restored database without its configuration is not a recovery.
- Logs follow their own retention pipeline (`LOGS.md`) and are not part of DB backups.

## Procedures

### Nightly logical backup

```sh
PGUSER=<user> PGPASSWORD=<from-secret-manager> \
  scripts/db/backup.sh <database> <backup_dir> [retention_days]
```

The script writes `<db>_<utc-stamp>.dump` plus `.sha256`, prunes by retention,
and never leaves a half-written archive looking complete (`.partial` staging).

### Restore (drill or disaster)

```sh
PGUSER=<user> PGPASSWORD=<from-secret-manager> \
  scripts/db/restore.sh <backup_dir>/<archive>.dump <target_database>
```

The script verifies the checksum, drops/recreates the target database, restores,
and prints phase timings. Record every run in the drill log below.

### Point-in-time recovery

1. Stop the application (or point it at a standby).
2. Take/mount the most recent physical base backup taken **before** the target time.
3. Configure recovery on the restored data directory:
   - PostgreSQL ≥ 12: `restore_command='cp /wal_archive/%f %p'`,
     `recovery_target_time='<timestamp>'`, `recovery_target_action='promote'` in
     `postgresql.auto.conf` / signal file `recovery.signal`.
4. Start PostgreSQL; it replays archived WAL up to the target and promotes.
5. Verify probe data (see drill), then re-point the application.

## Restore drill log

Every drill is executed against an isolated instance and timed end-to-end.

| Date | Environment | Dataset | Backup | Destroy | Restore | Verified | Operator |
|---|---|---|---|---|---|---|---|
| 2026-08-24 | compose Postgres 18 (local) | full schema (17 tables) + 5,000-row probe | OK, checksum written (136 KB archive, <1 s) | `dropdb` confirmed gone | checksum OK, prep 0 s, restore 0 s, total <1 s | 5,000/5,000 rows, 17/17 tables | automated agent |

Drill cadence: monthly, and after any change to backup scripting or storage
layout. Production drills additionally restore into a clean container from the
off-host archive location.

## Verification checklist after any restore

- [ ] Checksum verified by `restore.sh`.
- [ ] Row-count probe matches pre-destroy counts.
- [ ] `__EFMigrationsHistory` matches the deployed application version.
- [ ] Application smoke test passes against the restored database
      (health endpoint + one authenticated session flow).
