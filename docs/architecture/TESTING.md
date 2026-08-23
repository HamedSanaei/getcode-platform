# Testing strategy

## Test pyramid / portfolio

1. Domain/application unit tests for invariants and state transitions.
2. Architecture tests for dependency/module rules.
3. Persistence/integration tests against real PostgreSQL/Redis containers.
4. Provider contract tests applied to each adapter with deterministic fake HTTP responses.
5. API contract tests/OpenAPI compatibility.
6. Frontend component/interaction tests.
7. Browser E2E for critical purchase/payment/activation flows using fake provider/payment adapters.
8. Load/soak/failure-injection tests before production scale.

## High-risk regression cases

Every relevant workflow must test duplicates and ambiguity:

- duplicate create-order request;
- duplicate payment callback;
- timeout after provider reserve was transmitted;
- worker crash between provider side-effect and state persistence;
- outbox duplicate delivery;
- double refund request;
- simultaneous wallet debit;
- activation timeout/cancellation race;
- provider returning unexpected/malformed state.

“100% happy-path coverage” is weaker than a small set of strong invariant/failure tests.
