namespace GetCode.Application.Payments;

/// <summary>
/// M06-003: the Application-owned, NORMALIZED payment contract. Gateways are
/// behind this port only — their DTOs/signatures never leak past Infrastructure.
/// Outcomes are explicit and total: captured money is distinguishable from
/// failed and from UNKNOWN (ambiguous verification must never be blindly retried,
/// mirroring M04-006 purchase-safety discipline).
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Canonical key of this gateway (e.g. "fake", "zarinpal").</summary>
    string GatewayKey { get; }

    /// <summary>Create a payment intent for an order (idempotent per intent key).</summary>
    Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken);

    /// <summary>Verify whether the customer's payment actually completed.</summary>
    Task<PaymentVerification> VerifyAsync(string gatewayReference, CancellationToken cancellationToken);
}

public sealed record PaymentIntentRequest(
    Guid OrderId,
    decimal Amount,
    string Currency,
    string IdempotencyKey,
    string? ReturnUrl);

public enum PaymentIntentOutcome
{
    /// <summary>New intent created; redirect/pay URL issued.</summary>
    Created = 0,
    /// <summary>The same intent key already exists — return the original reference.</summary>
    Duplicate = 1,
    /// <summary>Gateway refused to create the intent (definitive).</summary>
    Rejected = 2,
}

public sealed record PaymentIntentResult(
    PaymentIntentOutcome Outcome,
    string? GatewayReference,
    string? PayUrl,
    string SafeErrorCode);

public enum PaymentVerificationOutcome
{
    /// <summary>Money definitively captured.</summary>
    Captured = 0,
    /// <summary>Payment definitively failed/cancelled.</summary>
    Failed = 1,
    /// <summary>State cannot be determined — needs reconciliation, never blind retry.</summary>
    Unknown = 2,
}

public sealed record PaymentVerification(
    PaymentVerificationOutcome Outcome,
    string GatewayReference,
    decimal? CapturedAmount,
    string? CapturedCurrency,
    string SafeErrorCode);
