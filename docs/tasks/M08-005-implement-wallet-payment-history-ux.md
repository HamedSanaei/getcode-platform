# M08-005: Implement wallet/payment history UX

- Status: **TODO**
- Milestone: **M08**
- Priority: **P1**
- Depends on: M05-003, M06-004

## Goal

Implement wallet/payment history UX.

## Penpot implementation reference

Map to `Customer / Wallet / Desktop` on `GetCode · 06 Customer Dashboard`; use the shared mobile shell and responsive/state contract referenced in `design/handoff/PENPOT_PAGE_MAP.md`.

## Acceptance criteria

- [ ] History reflects ledger/payment truth with pagination and stable references.
- [ ] Sensitive gateway metadata is not exposed.
- [ ] Host-aware return links and receipts use configured public URL resolver.

## Required verification

- [ ] API authorization tests
- [ ] visual tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
