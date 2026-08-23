# M01-001: Define UX sitemap and critical user flows in Penpot

- Status: **IN_PROGRESS**
- Milestone: **M01**
- Priority: **P0**
- Depends on: M00-009

## Goal

Define UX sitemap and critical user flows in Penpot.

## Acceptance criteria

- [x] Penpot file/workspace is recorded in design/penpot README.
- [x] Public browse -> quote -> checkout -> activation and customer dashboard flows are mapped.
- [x] Admin is a separate flow surface; RTL/mobile states are included.

## Required verification

- [ ] design review checklist
- [ ] live-reference public route inventory
- [ ] desktop/mobile reference evidence for every parity-scoped public route
- [ ] product-owner side-by-side approval, including documented intentional GetCode differences

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Implemented in Penpot file `c269caa0-e456-818c-8008-85a77340be64`. The canonical page/board/flow map is `design/handoff/PENPOT_PAGE_MAP.md`. No migration, environment-variable or runtime impact.

### Live-reference audit log

- 2026-08-24 initial pass: automated direct inspection was denied; the preserved dated public screenshot was used only as a reference source.
- 2026-08-24 retry: the product owner confirmed the live site loaded. The exact open tab was found with the Numberland page title and `https://numberland.ir/`, but automated DOM reading was denied by the browser safety layer; a separate read-only fetch also failed non-retryably.
- Interpretation: the site is not recorded as down. The blocker is automated evidence capture, so current boards cannot honestly be certified as pixel-parity with the live site.
- Closure path: attach a public-route inventory and full-page desktop/mobile captures, or record product-owner side-by-side approval with all intentional differences. Until then this task remains `IN_PROGRESS`.
