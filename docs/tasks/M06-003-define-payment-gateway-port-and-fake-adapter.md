# M06-003: Define payment gateway port and fake adapter

- Status: **DONE**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-001

## Goal

Define payment gateway port and fake adapter.

## Acceptance criteria

-[x] Application owns normalized payment intent/verification contract. (`IPaymentGateway` + PaymentIntentRequest/Result, PaymentVerification with explicit Captured/Failed/**Unknown** outcomes — ambiguity never silently retried, mirroring M04-006 discipline)
-[x] Fake gateway supports success/failure/duplicate/replay scenarios. (scriptable outcomes; duplicate intent keys resolve to one reference; verification replays are stable and never flip state)
-[x] Gateway DTO/signatures remain Infrastructure-only. (canonical records live in Application.Payments; fake only implements the port; real-gateway wire DTOs will stay inside their adapter folders per AGENTS rule 4)

## Required verification

-[x] payment contract tests (+ structural pin that port lives in Application namespace and fake implements it)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Application/Payments/IPaymentGateway.cs`: CreateIntentAsync (Created/Duplicate/Rejected) + VerifyAsync (Captured/Failed/Unknown). Unknown exists so M06-004 callback handling can route ambiguous verifications to reconciliation instead of blind retry.
- `Infrastructure/Payments/FakePaymentGateway.cs`: scriptable; registered as singleton IPaymentGateway in Infrastructure DI. Real gateway decision + credentials remain open (M06-004) - nothing here claims production readiness.
- Tests increased: backend 353 (+8 payment contract tests).