# M04-004: Implement offer normalization and short-lived availability cache

- Status: **TODO**
- Milestone: **M04**
- Priority: **P0**
- Depends on: M04-002, M03-004

## Goal

Implement offer normalization and short-lived availability cache.

## Acceptance criteria

- [ ] Offers normalize provider cost/currency/availability with observed timestamp.
- [ ] Cache expiry/staleness is explicit; purchase revalidates authoritative availability as needed.
- [ ] Redis loss degrades to provider/database path rather than corrupting truth.

## Required verification

- [ ] cache fallback tests
- [ ] stale offer tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
