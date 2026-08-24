#!/usr/bin/env bash
# GetCode logical restore (M10-004).
#
# Restores a pg_dump custom-format archive into a (re)created target database,
# timing each phase. Intended for restore drills and disaster recovery.
# Secrets come from the environment; never from this repository.
#
# Usage: restore.sh <archive> <target_database>
set -euo pipefail

# Connection via environment (PGHOST/PGPORT/PGUSER/PGPASSWORD); sane local defaults.
export PGUSER="${PGUSER:-postgres}"
export PGHOST="${PGHOST:-/var/run/postgresql}"
export PGPORT="${PGPORT:-5432}"

ARCHIVE="${1:?archive (.dump) required}"
DATABASE="${2:?target database required}"

if [[ ! -f "${ARCHIVE}" ]]; then
  echo "[restore] archive not found: ${ARCHIVE}" >&2
  exit 1
fi

echo "[restore] verifying checksum"
sha256sum -c "${ARCHIVE}.sha256"

echo "[restore] dropping/recreating '${DATABASE}'"
START=$(date +%s)
psql --dbname=postgres --command="SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${DATABASE}' AND pid <> pg_backend_pid();" >/dev/null
psql --dbname=postgres --command="DROP DATABASE IF EXISTS \"${DATABASE}\";" >/dev/null
psql --dbname=postgres --command="CREATE DATABASE \"${DATABASE}\";" >/dev/null
PREP_SECONDS=$(( $(date +%s) - START ))

echo "[restore] restoring archive"
RESTORE_START=$(date +%s)
# Objects restore owned by the connecting user; ownership mapping is a
# deployment-specific concern handled outside the drill.
pg_restore --dbname="${DATABASE}" --no-owner --no-privileges "${ARCHIVE}"
RESTORE_SECONDS=$(( $(date +%s) - RESTORE_START ))

TOTAL=$(( $(date +%s) - START ))
echo "[restore] prep=${PREP_SECONDS}s restore=${RESTORE_SECONDS}s total=${TOTAL}s"
echo "[restore] done — record these timings in docs/operations/BACKUP_RESTORE.md drill log"
