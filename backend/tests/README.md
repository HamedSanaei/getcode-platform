# Test strategy

- **UnitTests**: domain/application behavior, no network/database.
- **ArchitectureTests**: dependency and boundary invariants; expand whenever an architectural rule becomes enforceable.
- **IntegrationTests**: ASP.NET host + real infrastructure via containers is introduced in M00-005.
- **ProviderContractTests**: common behavioral suite every real provider adapter must pass, introduced before the first provider ships.
- **E2E**: browser-to-provider-fake critical flows, introduced before release.

A provider/payment implementation is incomplete without deterministic fake/test adapters and timeout/retry/idempotency tests.
