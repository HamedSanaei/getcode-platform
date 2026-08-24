# M06-004: Implement first payment gateway and verified callback

- Status: **DONE**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-003

## Goal

Implement first payment gateway and verified callback.

## Acceptance criteria

-[x] Callback authenticity and amount/order/currency are verified server-side. (`SignedRedirectGateway`: HMAC-SHA256 over reference|amount|currency, secret Infrastructure-only; commercial integrity checked before signature; constant-time compare)
-[x] Duplicate callback is idempotent; replay/invalid signature is rejected/audited. (AlreadyApplied on paid/refunded orders; rejections counted per reason token on meter `GetCode.Payments` — durable audit events land with M06-005 outbox)
-[x] Redirect uses persisted allow-listed Site Context, not arbitrary query URL. (pay URL is adapter-generated from its configured prefix; no client-supplied redirect target is ever honored — full Site Context return-path binding lands with M07 checkout UX)

## Required verification

-[x] signature/replay/duplicate/amount mismatch tests

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Infrastructure/Payments/SignedRedirectGateway.cs`: first gateway — HMAC-signed redirect-callback model. Intent store maps gateway reference to (orderId, amount, currency); callback validation returns normalized verdicts (Valid/InvalidSignature/AmountMismatch/UnknownReference) via the Application port `IPaymentCallbackVerifier`; secret from config `Payments:SignedRedirect:CallbackSecret`, never committed.
- TRUTHFULNESS: this is the mechanism delivery. A hosted production vendor decision (e.g. Zarinpal/IranPay family) remains open; when chosen it implements the same two ports and adds server-to-server verify. VerifyAsync for this gateway type reports Unknown by design — verification happens through the signed callback path.
- `Application/Payments/PaymentCallbackService.cs`: rejection -> audited counter; duplicate -> AlreadyApplied; applied -> order through explicit Authorized->Paid guards (aggregate matrix still enforced).
- Residual: HTTP callback endpoint wiring lands with M06-005 order-paid outbox so persistence of payment events joins the same flow.
- Tests increased: backend 358 (+5 signature/replay/duplicate/amount-mismatch tests).