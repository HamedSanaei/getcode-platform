# Log operations

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
