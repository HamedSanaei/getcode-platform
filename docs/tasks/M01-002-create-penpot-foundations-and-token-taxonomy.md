# M01-002: Create Penpot foundations and token taxonomy

- Status: **IN_PROGRESS**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M01-001

## Goal

Create Penpot foundations and token taxonomy.

## Acceptance criteria

- [x] Color/type/spacing/radius/grid/elevation foundations exist in Penpot; the token catalog contains 53 core tokens.
- [x] GetCode and PlusPremium-host brand differences are token sets, not forked components.
- [x] Accessible contrast/RTL conventions are documented.

## Required verification

- [x] token structure review
- [x] accessibility/RTL contract review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Implemented on `GetCode · 01 Foundations` and `GetCode · 10 Responsive & States`. Active token sets are `GetCode/Core` and `GetCode/Brand/GetCode`; `GetCode/Brand/PlusPremium` is the alternate brand set. No runtime impact. The 2026-08-24 Numberland retry produced no new readable live visual evidence, so no token changes were justified. Final product approval depends on the evidence/approval gate in M01-001; status remains `IN_PROGRESS`.
