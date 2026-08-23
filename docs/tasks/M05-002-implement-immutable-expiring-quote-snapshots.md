# M05-002: Implement immutable expiring quote snapshots

- Status: **TODO**
- Milestone: **M05**
- Priority: **P0**
- Depends on: M05-001

## Goal

Implement immutable expiring quote snapshots.

## Acceptance criteria

- [ ] Quote binds product, customer-visible price/currency and expiry/identity.
- [ ] Checkout rejects expired/tampered quote references and can refresh safely.
- [ ] Provider cost snapshot needed for operations is separated from customer price.

## Required verification

- [ ] quote expiry/tamper tests
- [ ] API integration tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
