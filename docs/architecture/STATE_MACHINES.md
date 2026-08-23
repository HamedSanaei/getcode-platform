# State-machine guidance

Exact states are finalized by their implementation tasks. The invariant is already fixed: important workflows use explicit legal transitions rather than unrelated booleans.

## Order conceptual progression

```text
Created -> AwaitingPayment -> Paid -> FulfillmentPending -> Fulfilling -> Completed
             |                 |             |                |
          Cancelled         Refunded      Failed/Review    Expired/Review
```

Do not infer “paid” from fulfillment, or “completed” from a provider response alone.

## Provider attempt

Track an attempt separately from the customer Order so retries/failover/reconciliation have identities and evidence:

```text
Prepared -> Sent -> Applied
                 -> DefinitelyNotApplied
                 -> Ambiguous -> ReconciledApplied / ReconciledNotApplied / ManualReview
```

## Activation

Conceptually:

```text
Reserved -> WaitingForMessage -> MessageReceived -> Completed
                     |                |
                  Expired          Cancelled (only if legal)
```

Provider vendor statuses are mapped into GetCode states in the adapter/application workflow; vendor strings never become the domain enum automatically.
