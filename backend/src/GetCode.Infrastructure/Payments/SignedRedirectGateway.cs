using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using GetCode.Application.Payments;
using Microsoft.Extensions.Options;

namespace GetCode.Infrastructure.Payments;

public sealed class SignedRedirectGatewayOptions
{
    public const string SectionName = "Payments:SignedRedirect";
    public bool Enabled { get; set; }
    public string CallbackSecret { get; set; } = string.Empty;
    public string PayUrlPrefix { get; set; } = "https://pay.getcode.internal";
}

/// <summary>
/// M06-004: FIRST gateway — a redirect-style gateway whose callback carries an
/// HMAC-SHA256 signature over (reference, amount, currency). The secret never
/// leaves Infrastructure; Application only sees the normalized validation
/// verdict. Verification of "did money really arrive" for this gateway type IS
/// the valid signature over the agreed commercial terms, so VerifyAsync reports
/// Captured exactly when a callback validated — server-to-server verify APIs of
/// hosted gateways are out of scope until a vendor decision lands.
/// </summary>
public sealed class SignedRedirectGateway : IPaymentGateway, IPaymentCallbackVerifier
{
    public const string GatewayKeyValue = "signed-redirect";

    private readonly ConcurrentDictionary<string, SignedIntent> _intents = new(StringComparer.Ordinal);
    private readonly byte[] _secret;

    public SignedRedirectGateway(IOptions<SignedRedirectGatewayOptions> options)
    {
        var secret = options.Value.CallbackSecret;
        _secret = Encoding.UTF8.GetBytes(secret);
        Enabled = options.Value.Enabled && secret.Length > 0;
        PayUrlPrefix = options.Value.PayUrlPrefix;
    }

    public bool Enabled { get; }
    public string PayUrlPrefix { get; }
    public string GatewayKey => GatewayKeyValue;

    private sealed record SignedIntent(Guid OrderId, decimal Amount, string Currency);

    /// <summary>HMAC-SHA256 over reference|amount|currency, hex-encoded.</summary>
    public static string Sign(byte[] secret, string reference, decimal amount, string currency)
    {
        using var hmac = new HMACSHA256(secret);
        var payload = $"{reference}|{amount:0.00}|{currency}";
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();
    }

    public Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        if (!Enabled)
        {
            return Task.FromResult(new PaymentIntentResult(PaymentIntentOutcome.Rejected, null, null, "gateway-disabled"));
        }

        if (request.Amount <= 0m)
        {
            return Task.FromResult(new PaymentIntentResult(PaymentIntentOutcome.Rejected, null, null, "invalid-amount"));
        }

        var reference = $"sg-{request.OrderId:N}";
        _intents[reference] = new SignedIntent(request.OrderId, request.Amount, request.Currency);
        return Task.FromResult(new PaymentIntentResult(
            PaymentIntentOutcome.Created, reference, $"{PayUrlPrefix}/{reference}", string.Empty));
    }

    Task<PaymentVerification> IPaymentGateway.VerifyAsync(string gatewayReference, CancellationToken cancellationToken) =>
        Task.FromResult(new PaymentVerification(
            PaymentVerificationOutcome.Unknown, gatewayReference, null, null,
            "verify-via-signed-callback"));

    public CallbackValidation ValidateCallback(string gatewayReference, decimal presentedAmount, string presentedCurrency, string? presentedSignature)
    {
        if (!_intents.TryGetValue(gatewayReference, out var intent))
        {
            return new CallbackValidation(CallbackValidationOutcome.UnknownReference, Guid.Empty, "unknown-reference");
        }

        // Commercial integrity first: the signed terms must match the presented ones.
        if (intent.Amount != presentedAmount || !string.Equals(intent.Currency, presentedCurrency, StringComparison.Ordinal))
        {
            return new CallbackValidation(CallbackValidationOutcome.AmountMismatch, Guid.Empty, "amount-mismatch");
        }

        var expected = Sign(_secret, gatewayReference, intent.Amount, intent.Currency);
        if (presentedSignature is null ||
            presentedSignature.Length != expected.Length ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(presentedSignature), Encoding.UTF8.GetBytes(expected)))
        {
            return new CallbackValidation(CallbackValidationOutcome.InvalidSignature, Guid.Empty, "invalid-signature");
        }

        return new CallbackValidation(CallbackValidationOutcome.Valid, intent.OrderId, string.Empty);
    }
}
