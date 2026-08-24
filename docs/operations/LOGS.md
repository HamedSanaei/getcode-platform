# Log operations

## Layout and semantics (M00-007)

- Services run with **TZ=UTC**; a log day is a UTC calendar day.
- Active files: `/data/logs/active/<service>/<instance>/<service>-<YYYYMMDD>.jsonl`
  (compact JSONL, 128 MiB size chunks roll with `_N` suffixes).
- Closed UTC days are gzip-archived by the Worker/API background service into
  `/data/logs/{YYYY}/{MM}/{service}/{YYYY-MM-DD}-{instance}[-N].jsonl.gz`.
- The archive pass is **idempotent**: completed archives are never rewritten. A crashed
  pass (gzip written, source not yet removed) self-heals on the next run — the archived
  byte count is verified before the leftover source is discarded; conflicting sources are
  preserved with a `log.archive.destination_exists` warning instead of being destroyed.
- Every event carries `service`, `instance`, `environment`, `appVersion`; request-scoped
  events carry `correlationId`/`traceId` from the API middleware. Forbidden sensitive
  property names (`authorization`, `password`, `otp`, …) are masked to
  `***redacted***` in the pipeline itself — see `RedactionEnricher` and
  `LoggingRedactionPolicy`.

## Durable volume behavior

Compose mounts the named volume `getcode-logs` at `/data/logs` for both API and Worker, so
active files and archives survive container recreation. The volume is independent of the
PostgreSQL/Redis volumes; wiping databases never removes logs. Because nothing caches or
references month directories beyond the archive pass itself, deleting one is safe while
services run (the archive pass recreates entries on demand).

## Inspect active API log

```bash
tail -f /data/logs/active/getcode-api/getcode-api-$(date -u +%Y%m%d).jsonl
```

## Inspect archived log

```bash
gzip -cd /data/logs/2026/08/getcode-api/2026-08-23-<instance>.jsonl.gz | less
```

Use `jq` when available:

```bash
gzip -cd ...jsonl.gz | jq 'select(.correlationId == "...")'
```

## Delete a historical month

After confirming backup/retention policy:

```bash
rm -rf /data/logs/2025/01
```

Do not delete `active/` while services are running. Do not use logs as an audit ledger; Audit has its own durable model.
