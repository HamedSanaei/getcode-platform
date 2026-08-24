namespace GetCode.Application.Payments;

/// <summary>
/// M06-004: capability port for gateways whose customer-facing redirect comes
/// back with a signed callback. Implementations own the secret and the
/// signature scheme (Infrastructure); the outcome contract stays normalized
/// here so callback handling is gateway-agnostic.
/// </summary>
public interface IPaymentCallbackVerifier
{
    /// <summary>
    /// Validate authenticity AND commercial integrity (amount/currency must be
    /// exactly what the intent was created with). Never trust client data.
    /// </summary>
    CallbackValidation ValidateCallback(string gatewayReference, decimal presentedAmount, string presentedCurrency, string? presentedSignature);
}

public sealed record CallbackValidation(
    CallbackValidationOutcome Outcome,
    Guid OrderId,
    string SafeErrorCode);

public enum CallbackValidationOutcome
{
    Valid = 0,
    InvalidSignature = 1,
    AmountMismatch = 2,
    UnknownReference = 3,
}
