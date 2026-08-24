# M00-007: Complete structured logging and archive verification

- Status: **DONE**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Complete structured logging and archive verification.

## Acceptance criteria

- [x] API and Worker write JSONL with service/environment/correlation context.
- [x] Closed UTC-day files gzip into `logs/YYYY/MM/<service>` and deleting a month folder is safe.
- [x] Archive operation is idempotent/crash-safe and durable volume behavior is documented.
- [x] Redaction policy is tested for forbidden fields.

## Required verification

- [x] logging unit tests
- [x] archive integration test
- [x] redaction tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `RedactionEnricher.cs` (new — masks forbidden property values at the pipeline so no sink can render a secret), `LoggingRedactionPolicy.cs` (forbidden set expanded to every AGENTS.md category: auth headers, cookies, JWT/bearer, provider/payment credentials, card data, OTP/SMS bodies), `StructuredLogging.cs` (wires the enricher into both API and Worker bootstrap loggers), `LogArchiveHostedService.cs` (UTC-day boundary instead of local time; crash self-heal: an existing gzip with matching uncompressed length completes the interrupted archive, conflicting sources preserved for investigation), `GetCode.Infrastructure.csproj` (`InternalsVisibleTo` the observability tests), new tests `LogArchiveTests` (5 scenarios incl. month-folder deletion safety), `StructuredLoggingOutputTests` (end-to-end JSONL context + redaction through the real file sink), strengthened `RedactionPolicyTests` (category coverage + case-insensitivity + phone-mask fail-closed), `docs/operations/LOGS.md` (layout/semantics/volume behavior).
- Verification: 30/30 observability tests green (logging unit tests, archive integration tests on real temp filesystems, redaction tests); full solution 46/46; format + build clean. The output test proves correlationId flows from LogContext and that `password`/`authorization` values never reach the JSONL file bytes.
- Decisions/assumptions: day boundary = UTC (compose already sets TZ=UTC); redaction enforcement is property-name based (message-text scanning rejected as fragile/slow); deliberate diagnostic modes must use approved sanitized field names per ADR-012.
- Migration/config/operations impact: none breaking; archives now keyed to UTC days (folders may shift vs previous local-time behavior — irrelevant pre-production).
- Next unblocked task: M00-008 (tracing/metrics foundation).
