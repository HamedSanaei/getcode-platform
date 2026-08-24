# ADR-016: First real providers — 5SIM for virtual numbers, Kavenegar for outbound user SMS

- Status: **Accepted**
- Date: 2026-08-24
- Decides: M04-002 (first real virtual-number provider) and M04-008 (outbound SMS notification abstraction + first adapter).

## Context

Two external integration decisions were resolved by the product owner:

1. The first real **virtual number supplier** is **5SIM** (current protocol).
2. The first **outbound user SMS provider** is **Kavenegar** (Iranian recipients).

These are fundamentally different domains:

| | Virtual number purchase | User SMS notification |
|---|---|---|
| Direction | GetCode rents a number from the supplier; the customer's OTP arrives on that rented number | GetCode sends an SMS to the customer's own phone |
| Port | `IVirtualNumberProvider` | `ISmsNotificationPort` |
| First adapter | 5SIM (`Infrastructure/Providers/FiveSim`) | Kavenegar (`Infrastructure/Notifications/Sms/Kavenegar`) |

## Decision

- **Strict separation.** The two integrations never share a port, namespace,
  DTO, or abstraction. An architecture test
  (`ProviderNotificationSeparationTests`) pins that neither adapter references
  the other's types.
- **5SIM specifics.** Current protocol only; no deprecated API1 fallback unless
  an operation is genuinely unavailable there (documented if it happens).
  Vendor DTOs/status strings stay internal to the adapter folder; canonical
  country keys map to vendor names via configuration.
- **Purchase safety.** 5SIM buy has no idempotency key. Transport failure after
  send ⇒ canonical `AmbiguousOutcome`; the idempotency key is recorded so
  same-key retries are refused until reconciliation proves non-duplication.
  Durable provider-operation state and reconciliation remain Application/
  Persistence concerns (M04-006).
- **Kavenegar specifics.** Verification codes use the official templated
  VerifyLookup flow (template name from configuration); plain transactional
  messages use sms/send. Normalized outcomes carry stable safe tokens and a
  transient-retryable flag; bounded retries belong to the dispatch layer
  (outbox worker, arriving with M06-005), never inside the adapter.
- **Credentials.** API keys/tokens are secrets/environment configuration only;
  opt-in registration (`Enabled` flags); explicit HTTP timeouts; typed
  HttpClient adapters over the official REST contracts rather than vendor SDKs
  (control over timeouts, resilience, telemetry, redaction, testing).
- **Redaction.** Provider tokens appear only in request authorization/path
  segments; safe error tokens are stable ASCII; OTP values are never logged;
  existing log-redaction policies apply.

## Consequences

- Business code depends only on the two ports; swapping providers requires no
  changes outside Infrastructure.
- Live smoke verification for both providers remains externally blocked until
  funded accounts exist; all CI runs offline/stubbed by design.
- M06-005 must raise notification requests through the outbox so financial
  transactions never wait on SMS HTTP calls.
