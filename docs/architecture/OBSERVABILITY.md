# Observability and logging

## Objectives

- enough structured context to reconstruct a failed order/provider/payment workflow;
- compact local storage;
- simple month-level retention by deleting a directory;
- safe for production: no secrets/OTP/raw SMS bodies;
- future-compatible with centralized log/tracing systems.

## File layout

Active files:

```text
logs/active/getcode-api/<instance>/getcode-api-20260824.jsonl
logs/active/getcode-worker/<instance>/getcode-worker-20260824.jsonl
```

Completed rolling days are gzip archived. Production containers use `TZ=UTC`, so production day boundaries are UTC:

```text
logs/
  2026/
    08/
      getcode-api/
        2026-08-23-<instance>.jsonl.gz
      getcode-worker/
        2026-08-23-<instance>.jsonl.gz
```

If a day rolls because of file-size limit, chunk suffixes are preserved. Deleting `logs/2026/08/` intentionally deletes that month's archive.

Automatic retention is disabled by default (`AutomaticRetentionMonths = 0`). It can be introduced later without breaking manual deletion.

## Event model

Use stable event names, for example:

```text
order.created
order.paid
wallet.debit.completed
provider.reserve.started
provider.reserve.failed
provider.reconciliation.required
activation.sms_received
payment.callback.rejected
refund.completed
```

Prefer structured properties over interpolating everything into message text.

## Useful context

Add only when relevant:

- `correlationId`, `traceId`, `requestId`;
- `userId`, `orderId`, `paymentId`, `ledgerEntryId`, `activationId`;
- `provider`, `providerOperationId` (safe external ID where allowed);
- canonical `countryKey`, `serviceKey`, `productKey`;
- `durationMs`, attempt/retry, normalized error code, HTTP status;
- service/environment/app version/host.

## Forbidden/default-redacted content

Never log by default:

- Authorization/Cookie headers;
- passwords/secrets/API keys;
- access/refresh tokens;
- payment credentials/card data;
- OTP values;
- raw SMS bodies;
- raw provider/payment request/response bodies;
- full phone number when a masked identifier suffices.

Provider adapters own safe telemetry projection; they must not serialize vendor DTOs wholesale into logs.

## Correlation

Incoming safe `X-Correlation-Id` may be accepted; otherwise generate one. Preserve it through API -> outbox/job -> provider attempt so support can trace a workflow without relying on message text search.

## Metrics/traces

OpenTelemetry is the intended compatibility layer. Introduce counters/histograms for provider success/latency, order transitions, payment callbacks, worker queue age and retry/reconciliation counts when those workflows exist.
