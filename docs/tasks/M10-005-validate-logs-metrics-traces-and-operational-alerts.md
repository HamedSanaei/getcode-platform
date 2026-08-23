# M10-005: Validate logs/metrics/traces and operational alerts

- Status: **TODO**
- Milestone: **M10**
- Priority: **P0**
- Depends on: M00-008, M10-002

## Goal

Validate logs/metrics/traces and operational alerts.

## Acceptance criteria

- [ ] Operators can trace order/payment/provider lifecycle using correlation IDs.
- [ ] Disk/log archive failure, worker backlog, provider degradation and payment anomalies have actionable signals.
- [ ] Alerts avoid raw sensitive data and have runbook links.

## Required verification

- [ ] observability drill
- [ ] log retention/archive drill

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
