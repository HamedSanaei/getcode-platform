# M08-002: Implement quote and checkout UX

- Status: **TODO**
- Milestone: **M08**
- Priority: **P0**
- Depends on: M06-002, M08-001

## Goal

Implement quote and checkout UX.

## Penpot implementation reference

Map to `Checkout / Desktop`, `Checkout / Mobile` and `Payment / Results` on `GetCode · 05 Auth & Checkout`; see `design/handoff/PENPOT_PAGE_MAP.md` for IDs and state coverage.

## Acceptance criteria

- [ ] UX handles quote expiry/refresh, insufficient wallet, payment-required and duplicate-submit safely.
- [ ] Client never treats locally calculated price/payment as authoritative.
- [ ] Loading/error/retry states follow Penpot handoff.

## Required verification

- [ ] browser duplicate-submit test
- [ ] visual/accessibility tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Progress (2026-08-24, in progress)

- Backend groundwork landed: `POST /api/checkout` (authenticated + CSRF) delegating to the M06-002 CheckoutService. Server revalidates the quote (never trusts client price); duplicate submits with a stable client-side idempotency key deterministically return the SAME order (`replayed=true`). Expired/unknown/tampered quotes map to explicit 410/404/409 responses.
- Integration test: full auth+CSRF handshake -> issue quote -> triple submit -> same orderId; tampered amount rejected 409.
- NEXT: React `/checkout` page mapping Penpot frames (Checkout / Desktop `...8776b3a59fe4`, Mobile `...8776b4fd84a1`, Payment / Results `...8776b4a88636`) incl. expiry countdown/refresh and duplicate-submit disabled state; then browser duplicate-submit test.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.
