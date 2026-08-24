#!/usr/bin/env bash
# GetCode logical backup (M10-004).
#
# Produces a compressed, custom-format pg_dump archive plus a SHA-256 checksum,
# then prunes old archives by retention class. Secrets come from the environment
# (PGHOST/PGPORT/PGUSER/PGPASSWORD or PGPASSFILE) — never from this repository.
#
# Usage: backup.sh <database> <backup_dir> [retention_days]
set -euo pipefail

# Connection via environment (PGHOST/PGPORT/PGUSER/PGPASSWORD); sane local defaults.
export PGUSER="${PGUSER:-postgres}"
export PGHOST="${PGHOST:-/var/run/postgresql}"
export PGPORT="${PGPORT:-5432}"

DATABASE="${1:?database required}"
BACKUP_DIR="${2:?backup dir required}"
RETENTION_DAYS="${3:-14}"

STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
TARGET_DIR="${BACKUP_DIR}/logical/${DATABASE}"
mkdir -p "${TARGET_DIR}"

ARCHIVE="${TARGET_DIR}/${DATABASE}_${STAMP}.dump"

echo "[backup] starting logical backup of '${DATABASE}'"
START=$(date +%s)
# Write to a partial name first so a failed run never leaves a plausible-looking archive.
pg_dump --dbname="${DATABASE}" --format=custom --compress=9 --file="${ARCHIVE}.partial"
mv "${ARCHIVE}.partial" "${ARCHIVE}"
SECONDS_ELAPSED=$(( $(date +%s) - START ))

sha256sum "${ARCHIVE}" > "${ARCHIVE}.sha256"
SIZE=$(du -h "${ARCHIVE}" | cut -f1)
echo "[backup] wrote ${ARCHIVE} (${SIZE}) in ${SECONDS_ELAPSED}s"

# Retention pruning: delete archives older than the window.
find "${TARGET_DIR}" -name '*.dump' -mtime "+${RETENTION_DAYS}" -print -delete |
  while read -r old; do echo "[backup] pruned ${old}"; rm -f "${old}.sha256"; done
# Remove stale partials (failed runs) older than a day.
find "${TARGET_DIR}" -name '*.partial' -mtime +1 -delete

echo "[backup] done"
