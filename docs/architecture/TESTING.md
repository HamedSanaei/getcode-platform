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

## CI gates and bypass policy (M00-004)

Required status checks on every push/PR (`ci.yml`, `codeql.yml`, `container.yml`):

- backend: locked-mode restore → format → build → vulnerable-package audit → tests;
- frontend: deterministic `npm ci` → npm audit → lint → typecheck → production build;
- containers: api/worker/web images build on every PR and tag.

Rules:

1. Determinism is enforced mechanically: NuGet lock files are committed and CI restores
   with `--locked-mode`; the frontend uses `npm ci`. Dependency drift cannot enter through
   "it restored fine on my machine".
2. No agent or contributor may delete, skip, weaken, or conditionally disable a gate to
   land a change. Root causes are fixed, never the signal.
3. Branch protection must mark these workflows as required checks (repository-admin
   action; recorded here as the standing governance requirement).
