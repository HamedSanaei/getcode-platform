# M06-002: Implement idempotent checkout/order creation

- Status: **DONE**
- Milestone: **M06**
- Priority: **P0**
- Depends on: M06-001, M05-004

## Goal

Implement idempotent checkout/order creation.

## Acceptance criteria

-[x] Duplicate client submit cannot create/pay two orders. (unique DB index on (customer, idempotency key); racing submits resolve to the same order — one creator, loser replays winner row; sequential duplicates short-circuit)
-[x] Request idempotency is scoped/authenticated and persisted durably. (key scoped per customer_id in PostgreSQL `orders` table via AddOrders migration)
-[x] Order creation does not call external provider while holding DB transaction. (checkout only validates the quote and persists the aggregate with its snapshot; provider reservation is a later compensated flow M07)

## Required verification

-[x] duplicate concurrent request integration tests (sequential replay + parallel Task.WhenAll race against real PostgreSQL)

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- Add no production secret/credential to repository or tests.
- Keep provider/payment DTOs contained in Infrastructure when applicable.
- Update `docs/STATUS.md` and this task status when completed.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/config/operations impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Domain: Order gains durable `IdempotencyKey` (required ctor arg).
- Application: CheckoutService.CreateOrderAsync — quote revalidated (not-found/expired/tampered fail fast), order built from the quote snapshot; duplicate insert caught as OrderAlreadyExistsException then resolved by reading the winners row.
- Persistence: OrderConfiguration (`orders` table, unique composite index customer_id+idempotency_key), OrderRepository mapping unique violations to OrderAlreadyExistsException; AddOrders migration applied to schema. Repository registered in Persistence DI (Infrastructure must not reference Persistence).
- Residual: QuoteService store remains in-memory until M06-004/M07 wire checkout into the request pipeline with auth context; orders themselves are fully durable and self-contained for audit.
- Tests increased: backend 345 (+2 integration idempotency tests on real PostgreSQL).