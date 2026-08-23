# M09-005: Implement order/payment/refund support tools

- Status: **TODO**
- Milestone: **M09**
- Priority: **P0**
- Depends on: M07-005, M09-001

## Goal

Implement order/payment/refund support tools.

## Acceptance criteria

- [ ] Support can trace order by safe identifiers/correlation context.
- [ ] Refund/manual resolution invokes idempotent application commands, never direct balance/table editing.
- [ ] Every privileged action records actor/reason/outcome audit event.

## Required verification

- [ ] privileged action authorization/idempotency/audit tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
