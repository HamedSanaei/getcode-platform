# M01-007: Establish visual regression harness

- Status: **TODO**
- Milestone: **M01**
- Priority: **P1**
- Depends on: M01-005, M01-006

## Goal

Establish visual regression harness.

## Penpot implementation reference

Use the representative boards listed in `design/handoff/PENPOT_PAGE_MAP.md`. Minimum baselines are public catalog desktop/mobile, auth desktop, activation state gallery, customer dashboard and the two brand samples on `GetCode · 10 Responsive & States`.

## Acceptance criteria

- [ ] Browser/component visual harness runs deterministically in CI.
- [ ] Representative Penpot-mapped components and both brand contexts have baselines.
- [ ] Baseline update procedure requires explicit review.

## Required verification

- [ ] visual regression CI run

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
