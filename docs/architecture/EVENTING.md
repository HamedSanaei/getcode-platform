# Events and outbox

## Event types

Use two concepts deliberately:

- **Domain events**: in-process facts raised by aggregates and meaningful inside the domain model.
- **Integration/outbox messages**: durable serialized messages used to continue asynchronous work or integrate with external systems.

Do not serialize arbitrary domain object graphs as public messages. Define stable message contracts with schema/version strategy when they become durable.

## Transactional Outbox

Business state and the intent to perform asynchronous work are committed in the same PostgreSQL transaction. The Worker leases unprocessed outbox rows, handles them at-least-once, and marks completion/retry state.

Consumers must therefore be idempotent.

## Naming examples

Stable structured event/log names can include:

```text
order.created
order.paid
fulfillment.requested
provider.reserve.started
provider.reserve.ambiguous
provider.reconciliation.completed
activation.message_received
activation.expired
wallet.debit.completed
payment.callback.verified
refund.completed
```

Logging event names and durable integration message type names may be related but are not automatically the same schema.

## Broker evolution

A broker is not required to get durable asynchronous semantics in v1. ADR-011 requires measured justification before adding RabbitMQ. If introduced, inbox/idempotent consumer semantics remain mandatory because brokers also redeliver.
