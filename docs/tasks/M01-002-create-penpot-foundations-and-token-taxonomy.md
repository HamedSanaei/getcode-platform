# M01-002: Create Penpot foundations and token taxonomy

- Status: **DONE**
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

Implemented on `GetCode · 01 Foundations` and `GetCode · 10 Responsive & States`. Active token sets are `GetCode/Core` and `GetCode/Brand/GetCode`; `GetCode/Brand/PlusPremium` is the alternate brand set. The live HTML/CSS audit confirms the expected RTL/mobile/brand-role structure and does not change this task's acceptance contract. All three acceptance criteria and both required reviews are satisfied, so this task is `DONE`. No runtime impact.
