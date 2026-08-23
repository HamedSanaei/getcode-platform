# M00-007: Complete structured logging and archive verification

- Status: **TODO**
- Milestone: **M00**
- Priority: **P0**
- Depends on: M00-001

## Goal

Complete structured logging and archive verification.

## Acceptance criteria

- [ ] API and Worker write JSONL with service/environment/correlation context.
- [ ] Closed UTC-day files gzip into `logs/YYYY/MM/<service>` and deleting a month folder is safe.
- [ ] Archive operation is idempotent/crash-safe and durable volume behavior is documented.
- [ ] Redaction policy is tested for forbidden fields.

## Required verification

- [ ] logging unit tests
- [ ] archive integration test
- [ ] redaction tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
