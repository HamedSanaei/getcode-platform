# Idempotency and reconciliation contract

This platform moves money and calls external providers. Retries are expected; duplicate side effects are not.

## Idempotency

Durable idempotency is required for at least:

- order creation / checkout submission;
- wallet debit/credit/refund;
- payment callback processing;
- provider reservation attempts where the provider supports an external key;
- outbox/consumer handling;
- privileged manual-resolution commands.

An idempotency record binds a key to authenticated scope + operation + semantic request fingerprint + final/intermediate result. Reusing a key with a conflicting semantic payload is rejected, not silently treated as the original request.

## External side-effect rule

Do not hold a PostgreSQL transaction open while waiting on a remote provider/payment HTTP call. Persist intent/attempt identity, perform the call, then reconcile the result into durable state using idempotent transitions.

## Ambiguity

A network timeout does not prove a provider/payment side effect failed. Classify outcomes as:

1. definitely not applied;
2. applied/verified;
3. ambiguous.

Ambiguous states use provider/gateway lookup/reconciliation or Manual Review. They do not trigger blind repeat purchasing/refunding.

## Worker crash model

Assume the process can die between any two statements. A workflow must remain correct if it crashes:

- after DB intent commit but before HTTP call;
- after HTTP side effect but before DB result commit;
- after outbox publish/dispatch but before marking processed;
- while a lease is held.

Tests should target these boundaries explicitly.
