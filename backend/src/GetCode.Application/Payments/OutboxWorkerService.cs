using GetCode.Domain.Orders;

namespace GetCode.Application.Payments;

/// <summary>
/// M06-005: worker-side processing loop for order-paid events. At-least-once
/// delivery: a crash between claim and completion re-delivers later, and
/// duplicate dispatch is absorbed by the idempotent handler (HandleOnce).
/// Dead letters are explicit, never silent.
/// </summary>
public interface IOutboxLeaseStore
{
    /// <summary>Claim the next unprocessed message (lease semantics), or null when empty.</summary>
    Task<OutboxClaim?> ClaimNextAsync(CancellationToken cancellationToken);

    Task MarkCompletedAsync(Guid messageId, CancellationToken cancellationToken);

    Task MarkFailedAsync(Guid messageId, string safeErrorCode, CancellationToken cancellationToken);

    Task MarkDeadLetteredAsync(Guid messageId, CancellationToken cancellationToken);
}

public sealed class OutboxWorkerService(
    IOutboxLeaseStore leaseStore,
    IOutboxDispatchHandler handler)
{
    public enum Dispatch { Handled = 0, AlreadyHandled = 1, Failed = 2, DeadLettered = 3 }

    /// <summary>Process one claimed message. Safe to call again after any crash.</summary>
    public async Task<Dispatch> ProcessNextAsync(CancellationToken cancellationToken)
    {
        var claim = await leaseStore.ClaimNextAsync(cancellationToken);
        if (claim is null)
        {
            return Dispatch.AlreadyHandled; // queue drained
        }

        if (OutboxRetryPolicy.IsDeadLetter(claim.AttemptCount))
        {
            await leaseStore.MarkDeadLetteredAsync(claim.MessageId, cancellationToken);
            return Dispatch.DeadLettered; // manual review — never silent
        }

        bool handled;
        try
        {
            handled = await handler.HandleOnceAsync(claim.MessageId, claim.Event, cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            var attempts = claim.AttemptCount + 1;
            await leaseStore.MarkFailedAsync(claim.MessageId,
                $"handler-failed:{attempts}:{OutboxRetryPolicy.RetryDelay(attempts).TotalSeconds}s", cancellationToken);
            return Dispatch.Failed;
        }

        await leaseStore.MarkCompletedAsync(claim.MessageId, cancellationToken);
        return handled ? Dispatch.Handled : Dispatch.AlreadyHandled;
    }
}
