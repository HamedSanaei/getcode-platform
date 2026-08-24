using GetCode.Domain.Orders;
using GetCode.Application.Providers;

namespace GetCode.Application.Fulfillment;

/// <summary>
/// M07-002: bridges the M07-001 lease queue to the M04-006 reservation
/// orchestrator. Guarantees:
/// - the provider call runs OUTSIDE any DB transaction (this layer only reads
///   the order, calls the port, then persists outcomes);
/// - the durable attempt identity is the JOB id ("fulfillment:{jobId}") — the
///   same across retries of one job, so the orchestrator's duplicate-purchase
///   guard holds across worker restarts;
/// - an AMBIGUOUS outcome can never cause another provider contact: every retry
///   of that job short-circuits in the orchestrator's blocked-key guard (zero
///   provider calls) and the job walks to DeadLettered / Manual Review.
/// </summary>
public sealed class ReservationOrchestrationService(
    IFulfillmentJobStore jobs,
    Orders.IOrderRepository orders,
    ProviderReservationOrchestrator orchestrator)
{
    public enum Outcome { Reserved = 0, RetryableFailure = 1, AmbiguousManualReview = 2 }

    /// <summary>
    /// Process one claimed job. Candidates come from the caller (routing policy
    /// output); the provider call itself happens before any persistence.
    /// </summary>
    public async Task<Outcome> ProcessAsync(
        FulfillmentJob job,
        ProviderSearchQuery? query,
        string offerKey,
        IReadOnlyList<string> orderedCandidateKeys,
        CancellationToken cancellationToken)
    {
        var order = await orders.FindByIdAsync(job.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("orchestration-order-missing");

        // Idempotent reconciliation: already-reserved jobs reconcile as no-op.
        if (order.FulfillmentState == OrderFulfillmentState.Reserved ||
            order.FulfillmentState == OrderFulfillmentState.Completed)
        {
            await jobs.CompleteAsync(job.Id, cancellationToken);
            return Outcome.Reserved;
        }

        // Durable attempt identity — stable per job across restarts/retries.
        var idempotencyKey = $"fulfillment:{job.Id}";
        var attempt = orchestrator.ReserveAcrossCandidates(
            orderedCandidateKeys, query, idempotencyKey, offerKey);

        switch (attempt.State)
        {
            case ProviderReservationOrchestrator.AttemptState.Applied:
                // Orders arrive here already paid (M06-005 flow); reconcile the
                // reservation into fulfillment state only.
                order.StartFulfillment();
                order.MarkProviderReserved(attempt.Reservation!.ProviderOperationId);
                await orders.UpdateAsync(order, cancellationToken);
                await jobs.CompleteAsync(job.Id, cancellationToken);
                return Outcome.Reserved;

            case ProviderReservationOrchestrator.AttemptState.Ambiguous:
                // NEVER re-contact providers for this job: straight to the
                // manual-review terminal state (no retry loop at all).
                await jobs.MarkDeadLetteredAsync(job.Id, cancellationToken);
                return Outcome.AmbiguousManualReview;

            default: // DefinitelyNotApplied: safe to retry later with fresh attempts
                await jobs.FailAsync(job.Id, DateTimeOffset.UtcNow, cancellationToken);
                return Outcome.RetryableFailure;
        }
    }
}
