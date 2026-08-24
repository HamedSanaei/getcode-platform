# M04-008: Implement SMS notification abstraction and Kavenegar adapter

- Status: **DONE**
- Milestone: **M04**
- Priority: **P1**
- Depends on: —

## Goal

Introduce the outbound user-SMS notification abstraction (semantic operations,
normalized outcomes) and implement its first real provider adapter,
**Kavenegar**, for Iranian recipients. This is deliberately a DIFFERENT concept
from virtual-number provisioning (`IVirtualNumberProvider` / 5SIM) and must
never share an abstraction with it.

## Acceptance criteria

- [ ] Application owns a semantic port (e.g. send verification code / send
      transactional SMS); Domain/Application never reference Kavenegar types.
- [ ] Kavenegar adapter lives under `Infrastructure/Notifications/Sms/Kavenegar`;
      credentials/sender/template names are secret/config-driven; explicit HTTP
      timeout; typed HttpClient; user-agent set.
- [ ] Normalized outcomes cover at least accepted/rejected/invalid-recipient/
      invalid-template/authentication-failed/rate-limited/provider-unavailable/
      timeout/unknown, each with stable safe tokens and a transient-retryable
      flag (only transient outcomes are retryable).
- [ ] Verification messages use Kavenegar's templated VerifyLookup flow;
      OTP generation/expiry/attempt limits remain GetCode responsibilities.
- [ ] Redaction: API key never appears in URLs/results/logs; OTP values are
      never logged by the adapter.

## Required verification

- [x] stubbed-HTTP tests for every outcome above (no production secrets)
- [x] phone-normalization tests (canonical Iranian mobile representation)
- [x] architecture test pinning that Application/Domain never reference the
      Kavenegar namespace

## Engineering constraints

- Follow root `AGENTS.md` and all accepted ADRs.
- No production secrets in source/tests.
- Async notification dispatch through the transactional outbox arrives with the
  order/payment flows (M06-005); this task delivers the transport + contract so
  those flows only need to raise a notification request.

## Agent handoff

Record: files changed, decisions/assumptions, commands/tests run, migration/
config/operational impact, residual risk and next unblocked task.

### Handoff (2026-08-24)

- Port: `Application/Notifications/ISmsNotificationPort` — semantic operations (`SendVerificationCodeAsync`, `SendTransactionalSmsAsync`), normalized `SmsDeliveryOutcome`s with stable safe tokens and a transient-retryable flag. Domain/Application never reference Kavenegar (pinned by existing layer tests + new `ProviderNotificationSeparationTests` which also guarantees the 5SIM and Kavenegar adapters never reference each other).
- Adapter: `Infrastructure/Notifications/Sms/Kavenegar/KavenegarSmsNotificationSender` — templated VerifyLookup for codes, sms/send for transactional messages; typed HttpClient with explicit timeout + user-agent; opt-in config (`Kavenegar:Enabled/ApiKey/Sender/VerificationTemplate/BaseUrl/TimeoutSeconds`). Vendor status families map defensively to canonical outcomes; exact code list revisited at live verification.
- Normalization: `IranianMobileNumber.Normalize` in Application — one canonical rule ("+98XXXXXXXXXX", accepts +98/98/0098/0 prefixes and Persian digits); adapters never duplicate it.
- Retry policy deliberately NOT inside the adapter: classification only. The future outbox worker (M06-005+) applies bounded retries to `IsTransientlyRetryable` results only; ambiguous duplicate-send prevention rides the same persisted-notification design.
- Tests increased: backend 269 (+28 unit: outcome matrix incl. auth/recipient/template/rate-limit/5xx/timeout/malformed, retry flags, redaction of API key & OTP, normalization table; +1 architecture separation test).
- Live verification: externally blocked until a funded Kavenegar account exists; everything else offline/stubbed.