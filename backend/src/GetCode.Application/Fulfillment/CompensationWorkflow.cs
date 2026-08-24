using GetCode.Application.Orders;
using GetCode.Application.Providers;
using GetCode.Application.Wallets;
using GetCode.Domain.Orders;

namespace GetCode.Application.Fulfillment;

/// <summary>
/// M07-004: compensation workflow for expired/cancelled reservations.
/// Rules baked in:
/// - refunds happen ONLY under policy: captured funds + uncompleted
///   fulfillment + (when a provider reservation exists) a DEFINITIVE
///   successful provider cancellation — ambiguity goes to reconciliation;
/// - refund idempotency key is `refund:{orderId}` so retries/replays can
///   never double-credit (ledger-level dedupe);
/// - races with message arrival are arbitrated by the ORDER STATE MACHINE,
///   not by checks-then-acts: a lost race surfaces as an explicit outcome.
/// </summary>
public sealed class CompensationWorkflow(
    IVirtualNumberProvider provider,
    WalletService wallet,
    Orders.IOrderRepository orders)
{
    public enum Outcome
    {
        Refunded = 0,
        AlreadyRefunded = 1,      // idempotent replay
        RejectedCompleted = 2,    // customer received the goods — nothing to compensate
        NoProviderReservation = 3,// nothing at the provider; straight refund
        ReconciliationRequired = 4,// cancel failed/ambiguous — NEVER auto-refund
        RaceLostMessageArrived = 5,// message arrived while we compensated
        RefundFailedRetryable = 6,// wallet write failed transiently; safe to retry
    }

    /// <summary>Stable refund idempotency identity per order.</summary>
    public static string RefundKey(Guid orderId) => $"refund:{orderId}";

    public async Task<Outcome> CancelAndRefundAsync(Order order, CancellationToken cancellationToken)
    {
        // Idempotency first: an already-refunded order replays as no-op.
        if (order.PaymentState == OrderPaymentState.Refunded)
        {
            return Outcome.AlreadyRefunded;
        }

        if (order.FulfillmentState == OrderFulfillmentState.Completed)
        {
            return Outcome.RejectedCompleted; // customer got what they paid for
        }

        var needsProviderCancel = order.ProviderOperationId is not null &&
            order.FulfillmentState is OrderFulfillmentState.Reserved or OrderFulfillmentState.Completed;

        if (needsProviderCancel && order.ProviderOperationId is { } operationId)
        {
            var cancel = await provider.CancelAsync(operationId, cancellationToken);
            if (!cancel.IsSuccess)
            {
                // Definitive failure OR ambiguity: the provider may still hold
                // (or have released) the number — we do NOT know. Auto-refunding
                // here could credit a customer while the purchase stands.
                return Outcome.ReconciliationRequired;
            }
        }

        // State-machine arbitration BEFORE money moves: a concurrent message
        // arrival makes these throw and NOTHING has been credited.
        try
        {
            order.FailFulfillment();
            order.MarkRefunded();
        }
        catch (InvalidOrderTransitionException)
        {
            return Outcome.RaceLostMessageArrived;
        }

        // Policy passed: apply the compensating credit (ledger-idempotent).
        var mutation = await wallet.RefundAsync(new WalletMutationCommand(
            order.CustomerId,
            Domain.Wallets.LedgerEntryType.Refund,
            order.Amount,
            RefundKey(order.Id),
            ReferenceType: "order",
            ReferenceId: order.Id,
            Currency: order.Currency), cancellationToken);

        if (!mutation.Success && mutation.FailureReason != "idempotency_conflict")
        {
            // Credit failed: the aggregate mutations above are NOT persisted
            // (no UpdateAsync), so durable state stays consistent; safe to retry.
            return Outcome.RefundFailedRetryable;
        }

        await orders.UpdateAsync(order, cancellationToken);
        return Outcome.Refunded;
    }
}
