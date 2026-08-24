using System.Diagnostics.Metrics;
using GetCode.Domain.Orders;

namespace GetCode.Application.Payments;

/// <summary>
/// M06-004: verified-callback handling. Authenticity and commercial integrity
/// are validated by the gateway adapter BEFORE any order mutation; duplicates
/// are idempotent; every rejection reason is counted for audit. The order
/// aggregate's own transition guards are the second line of defense.
/// </summary>
public sealed class PaymentCallbackService(IPaymentCallbackVerifier verifier, Orders.IOrderRepository orders)
{
    public const string MeterName = "GetCode.Payments";

    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> CallbackCounter =
        Meter.CreateCounter<long>("payment.callbacks", description: "Payment callback outcomes");

    public enum Handling { Applied = 0, AlreadyApplied = 1, Rejected = 2 }

    public sealed record CallbackHandlingResult(Handling Outcome, string ReasonToken, Guid OrderId);

    public async Task<CallbackHandlingResult> HandleCallbackAsync(
        string gatewayReference, decimal presentedAmount, string presentedCurrency, string? signature,
        CancellationToken cancellationToken)
    {
        var validation = verifier.ValidateCallback(gatewayReference, presentedAmount, presentedCurrency, signature);
        if (validation.Outcome != CallbackValidationOutcome.Valid)
        {
            // Invalid signature / forged replay / amount mismatch: rejected + audited.
            CallbackCounter.Add(1, new KeyValuePair<string, object?>("outcome", "rejected"),
                new KeyValuePair<string, object?>("reason", validation.SafeErrorCode));
            return new CallbackHandlingResult(Handling.Rejected, validation.SafeErrorCode, Guid.Empty);
        }

        var order = await orders.FindByIdAsync(validation.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("callback-order-missing"); // data integrity fault, not a customer error

        if (order.PaymentState is OrderPaymentState.Paid or OrderPaymentState.Refunded)
        {
            // Duplicate callback: idempotent, no double application.
            CallbackCounter.Add(1, new KeyValuePair<string, object?>("outcome", "duplicate"));
            return new CallbackHandlingResult(Handling.AlreadyApplied, "already-paid", order.Id);
        }

        // Gateway captured → authorize then capture through the explicit guards.
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        await orders.UpdateAsync(order, cancellationToken);

        CallbackCounter.Add(1, new KeyValuePair<string, object?>("outcome", "applied"));
        return new CallbackHandlingResult(Handling.Applied, "paid", order.Id);
    }
}
