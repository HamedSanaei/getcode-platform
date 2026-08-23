# Provider integration architecture

## Goal

Adding a provider should be localized and testable. Removing a provider should not require editing order/wallet/catalog business code.

## Adapter shape

```text
Application IVirtualNumberProvider
          ▲
          │ normalized GetCode contracts
Infrastructure/Providers/ProviderA
  - API client
  - auth
  - provider DTOs
  - mapping
  - error normalization
  - redacted telemetry
  - contract tests
```

## Canonical mapping

Example only:

```text
GetCode service key: telegram
  Provider A -> "tg"
  Provider B -> 291
  Provider C -> "telegram"
```

Mappings are data/config owned by GetCode, versioned/audited where operators can change them.

## Required adapter behaviors

Every adapter eventually passes a common provider contract suite for:

- availability/search normalization;
- reserve success;
- unavailable offer;
- auth failure;
- insufficient provider balance;
- rate limiting;
- timeout/cancellation propagation;
- malformed/unknown response;
- activation polling;
- cancellation;
- retry classification;
- safe logs/redaction.

## Ambiguous reservation rule

A timeout after a reserve request is not equivalent to “provider did nothing”. The adapter/router must classify an operation as:

- definitely not applied;
- applied;
- ambiguous/requires reconciliation.

Blind failover on ambiguous reserve can buy multiple numbers and is forbidden.
