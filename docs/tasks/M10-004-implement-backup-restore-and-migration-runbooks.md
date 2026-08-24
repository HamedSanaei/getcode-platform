# M10-004: Implement backup, restore and migration runbooks

- Status: **DONE**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M00-006

## Goal

Implement backup, restore and migration runbooks.

## Acceptance criteria

- [x] Automated PostgreSQL backup/PITR strategy is configured for target environment.
- [x] Restore drill succeeds and is timed/documented.
- [x] Deployment migration and rollback/forward-fix procedure is tested.

## Required verification

- [x] restore drill evidence
- [x] migration rehearsal

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed:
  - `scripts/db/backup.sh`: compressed custom-format `pg_dump` + SHA-256 checksum + retention pruning; `.partial` staging so failed runs never leave a plausible archive; secrets via environment only.
  - `scripts/db/restore.sh`: checksum verification, drop/recreate, phase-timed `pg_restore`, prints timings for the drill log.
  - `scripts/db/migrate-rehearsal.sh`: forward apply of the idempotent schema, core-table probes (users/sessions/wallets/wallet_entries/roles/outbox_messages/providers), simulated forward-fix, idempotent re-run.
  - `infrastructure/postgres/compose.pitr.yaml`: WAL-archiving enablement (`wal_level=replica`, `archive_mode=on`, `archive_command` to durable volume, `archive_timeout=60s`) with an entrypoint that hands the archive volume to the postgres user; verified live (`pg_stat_archiver`: archived 3, failed 0).
  - `docs/operations/BACKUP_RESTORE.md`: full runbook — layered strategy (nightly logical + continuous PITR + physical base backups), RPO ≤5 min / RTO ≤1 h targets, PITR procedure, drill log with today's evidence, post-restore checklist.
  - `docs/operations/MIGRATIONS.md`: deployment runbook — forward-only policy backed by expand-only migrations, required pre-deployment rehearsal with log, controlled deploy steps, rollback vs forward-fix decision rules, reviewer compatibility contract.
  - `docs/operations/sql/0001_baseline.sql`: regenerated idempotent script (migrations through `AddSessions`) used by the rehearsal.
- Live evidence (compose Postgres 18):
  - Restore drill: seeded full schema + 5,000-row probe → backup OK (checksum) → `dropdb` confirmed destroyed → restore verified checksum, prep 0 s, restore 0 s (<1 s total at drill scale) → probe verified 5,000/5,000 rows and 17/17 tables. Recorded in BACKUP_RESTORE.md drill log; production timing scales with volume size against the 1 h RTO budget.
  - PITR: override applied, archiving verified via `pg_switch_wal()` → segments land in `/wal_archive` (3 archived / 0 failed); `pg_basebackup` produced a 62.8 MB physical base backup, completing the PITR chain.
  - Migration rehearsal: PASS on scratch DB through `AddSessions`.
- Decisions/assumptions: logical dumps are the portable safety net while WAL archiving provides the low-RPO layer; Redis excluded from DR truth per architecture contract; rollback = previous app version over expand-only schema (never down-migrate).
- Verification commands: the three scripts executed inside the compose container; `python scripts/verify_starter.py` OK (now excluding ~31.5k generated/vendor files after this round's fix).
- Migration/config/operations impact: production must schedule the two scripts (cron/systemd), mount the WAL archive on durable storage, and set `Cors:…`-style env-based credentials via secret manager. No application code changes.
- Residual risk: off-host archive replication (object storage sync) is environment provisioning, not scriptable from this repo; documented in runbook. Production-scale RTO measurement pending real data volume.
- Next unblocked tasks: M02-005 (SSO v1 scope decision), M01-007 (visual harness infra).