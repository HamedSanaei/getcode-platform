# M00-008: Add tracing/metrics foundation

- Status: **DONE**
- Milestone: **M00**
- Priority: **P1**
- Depends on: M00-007

## Goal

Add tracing/metrics foundation.

## Acceptance criteria

- [x] OpenTelemetry-compatible ActivitySource/Meter naming convention is documented and wired.
- [x] Trace/correlation context can flow API -> durable job/outbox metadata.
- [x] No high-cardinality sensitive labels are used.

## Required verification

- [x] trace propagation test
- [x] metric naming review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Files changed: `GetCode.Application/Common/GetCodeObservability.cs` (new — shared `GetCode.Core` ActivitySource + Meter, `CaptureTraceContext()`, documented naming/tag conventions), `OutboxMessage.cs` (`trace_id`/`span_id` columns + `OutboxMessage.Create` factory stamping W3C context and UUIDv7 id), `OutboxMessageConfiguration.cs` (bounded varchar 32/16), migration `*_AddTraceContextToOutbox` (expand-only, nullable), tests: `TracePropagationTests.cs` (integration — context captured inside a real ActivityListener scope persists through Npgsql roundtrip; absent-activity case tolerated), `ObservabilityNamingTests.cs` (naming review + W3C id format + capture semantics), `SchemaShapeTests.cs` (expected columns extended in physical order), `docs/architecture/OBSERVABILITY.md` (conventions + tag policy + durable-work rule).
- Decisions: BCL primitives only (no OpenTelemetry SDK yet — exporters are M10-005); tags policy forbids unbounded/sensitive labels and is documented as the review gate; trace ids ride nullable columns so existing rows remain valid.
- Verification: unit 5/5, integration 9/9 (incl. new propagation test), full solution 52/52 green; format/build clean.
- Migration/config/operations impact: one additive migration; no config changes.
- Next unblocked task: M00-009 (governance gate) completes milestone M00.
