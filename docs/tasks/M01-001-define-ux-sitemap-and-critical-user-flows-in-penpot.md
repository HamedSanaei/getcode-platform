# M01-001: Define UX sitemap and critical user flows in Penpot

- Status: **DONE**
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

- [x] design review checklist
- [x] live HTML structural route-family inventory
- [x] Penpot board/page-map mapping review

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Implemented in Penpot file `c269caa0-e456-818c-8008-85a77340be64`. The canonical page/board/flow map is `design/handoff/PENPOT_PAGE_MAP.md`. No migration, environment-variable or runtime impact.

### Live-reference audit log

- 2026-08-24: live homepage HTML was downloaded directly with curl and hashed. It exposed 163 unique internal routes across 20 route families.
- Seventeen representative pages discovered from those links were downloaded successfully with HTTP 200.
- The HTML/CSS structure confirms the RTL public navigation, product rail, ordinary/rental/permanent number tabs, country/service selection, login/register, purchase/payment/refund messaging and content/support families mapped in Penpot.
- Full evidence and the mapping verdict are recorded in `design/handoff/NUMBERLAND_LIVE_HTML_AUDIT_2026-08-24.md`.
- Penpot was saved as `GetCode Design System v1.1 — live HTML validation`; file validation reports zero errors at revision 104.
- Visual differences that cannot be settled from HTML/CSS and the preserved screenshot remain optional owner-review items, not undocumented acceptance blockers.

All documented acceptance criteria and required verification are satisfied. No migration, environment-variable or runtime impact.
