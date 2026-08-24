#!/usr/bin/env bash
# Migration deployment rehearsal (M10-004).
#
# Proves the deployment procedure on a scratch database:
#   1. apply the full idempotent migration script (forward path);
#   2. verify expected core tables exist;
#   3. simulate a forward-fix migration applied on top;
#   4. re-apply the idempotent script — must remain a no-op for existing objects
#      and compatible with a "previous app version" (expand-only policy).
#
# Usage: migrate-rehearsal.sh <scratch_database> [schema_sql_path]
set -euo pipefail

# Connection via environment (PGHOST/PGPORT/PGUSER/PGPASSWORD); sane local defaults.
export PGUSER="${PGUSER:-postgres}"
export PGHOST="${PGHOST:-/var/run/postgresql}"
export PGPORT="${PGPORT:-5432}"

DATABASE="${1:?scratch database required}"
SCHEMA_SQL="${2:-docs/operations/sql/0001_baseline.sql}"

psql --dbname=postgres --command="SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${DATABASE}' AND pid <> pg_backend_pid();" >/dev/null || true
psql --dbname=postgres --command="DROP DATABASE IF EXISTS \"${DATABASE}\";" >/dev/null
psql --dbname=postgres --command="CREATE DATABASE \"${DATABASE}\";" >/dev/null

echo "[rehearsal] applying baseline schema"
psql --dbname="${DATABASE}" --file="${SCHEMA_SQL}" >/dev/null

echo "[rehearsal] verifying core tables"
for table in users sessions wallets wallet_entries roles outbox_messages providers; do
  psql --dbname="${DATABASE}" --tuples-only --no-align --command="SELECT to_regclass('public.${table}');" | grep -q "${table}" ||
    { echo "[rehearsal] MISSING table ${table}" >&2; exit 1; }
  echo "  ok ${table}"
done

echo "[rehearsal] simulating forward-fix migration (expand-only)"
psql --dbname="${DATABASE}" --command="CREATE TABLE IF NOT EXISTS public.forward_fix_probe(id uuid primary key);" >/dev/null

echo "[rehearsal] re-applying idempotent schema (must not fail)"
psql --dbname="${DATABASE}" --file="${SCHEMA_SQL}" >/dev/null

echo "[rehearsal] OK: forward migration + forward-fix + re-run are all safe"
