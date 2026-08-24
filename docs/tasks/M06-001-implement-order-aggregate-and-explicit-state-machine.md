# M06-001: Implement Order aggregate and explicit state machine

- Status: **DONE**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M05-002

## Goal

Implement Order aggregate and explicit state machine.

## Acceptance criteria

-[x] Allowed transitions are explicit; invalid transitions fail deterministically. (closed transition dictionaries; every illegal move throws `InvalidOrderTransitionException` with a `order-transition-forbidden:{from}->{to}` token)
-[x] Order stores immutable commercial snapshot/reference needed for support/audit. (amount/currency/product identity/pricing-rule version/quote id fixed at creation; state changes never touch it - test-pinned)
-[x] State names separate payment from fulfillment outcomes. (`OrderPaymentState` AwaitingPayment/PaymentAuthorized/Paid/PaymentFailed/Refunded vs `OrderFulfillmentState` NotStarted/Reserving/Reserved/Completed/Failed; paid-but-unfulfilled is expressible; provider failure never implies money lost)

## Required verification

-[x] state transition matrix tests (+ payment-gate: fulfillment throws until capture; terminal states closed)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- `Domain/Orders/Order.cs`: pure-Domain aggregate, zero infrastructure deps. Payment and fulfillment are two explicit dimensions with their own matrices; refunds only from captured money; ProviderOperationId recorded at reservation for audit linkage to M04-006 reconciliation entries.
- Idempotent replays modeled as self-transitions (e.g. duplicate Paid callback) rather than errors.
- Persistence mapping (EF configuration, concurrency tokens) lands with M06-002 idempotent checkout, which will persist orders in the same transaction as quote consumption.
- Tests increased: backend 343 (+22 matrix/gate/immutability tests).