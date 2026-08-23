# M01-004: Implement Penpot-to-code design token bridge

- Status: **TODO**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-002, M00-001

## Goal

Implement Penpot-to-code design token bridge.

## Penpot implementation reference

- Canonical file and token-set names are recorded in `design/penpot/README.md` and `design/handoff/PENPOT_PAGE_MAP.md`.
- Source sets are `GetCode/Core`, `GetCode/Brand/GetCode` and `GetCode/Brand/PlusPremium`.
- The bridge must preserve semantic token names and generate/select a host brand without forking components.

## Acceptance criteria

- [ ] Approved token snapshot is stored/versioned in design/tokens.
- [ ] Next.js CSS/theme values are generated or deterministically mapped from tokens.
- [ ] CI detects invalid token schema/drift according to documented policy.
- [ ] Snapshot metadata records the canonical Penpot file ID and source revision used for generation.

## Required verification

- [ ] token validation test
- [ ] frontend build

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
