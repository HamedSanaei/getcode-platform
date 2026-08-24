using System.Collections.Concurrent;
using GetCode.Application.Payments;

namespace GetCode.Infrastructure.Payments;

/// <summary>
/// M06-003: scriptable fake gateway for tests and local development. Supports
/// success, definitive failure, duplicate intent keys and verification replays.
/// Wire-level behavior of real gateways (redirect flows, callback signatures)
/// is deliberately out of scope here — this adapter exists so the checkout
/// pipeline can be built and tested before a real gateway decision lands.
/// </summary>
public sealed class FakePaymentGateway : IPaymentGateway
{
    public const string GatewayKeyValue = "fake";

    public string GatewayKey => GatewayKeyValue;

    private readonly ConcurrentDictionary<string, FakeIntent> _intents = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, PaymentVerification> _verifications = new(StringComparer.Ordinal);
    private readonly Queue<PaymentVerificationOutcome> _scriptedVerifyOutcomes = new();

    private sealed record FakeIntent(string Reference, decimal Amount, string Currency);

    public string PayUrlPrefix { get; set; } = "https://pay.fake.test";

    /// <summary>Script the NEXT verification outcomes (consumed in order; default Captured when empty).</summary>
    public void QueueVerification(PaymentVerificationOutcome outcome) => _scriptedVerifyOutcomes.Enqueue(outcome);

    /// <summary>Pre-seed a verification result, e.g. to test replayed callbacks.</summary>
    public void SeedVerification(string gatewayReference, PaymentVerification verification) =>
        _verifications[gatewayReference] = verification;

    public Task<PaymentIntentResult> CreateIntentAsync(PaymentIntentRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0m)
        {
            return Task.FromResult(new PaymentIntentResult(
                PaymentIntentOutcome.Rejected, null, null, "invalid-amount"));
        }

        var reference = $"fake-{request.OrderId:N}-{request.IdempotencyKey}";
        var existing = _intents.GetOrAdd(reference, _ => new FakeIntent(reference, request.Amount, request.Currency));

        // Duplicate intent keys resolve to the SAME gateway reference.
        return Task.FromResult(existing.Reference == reference
            ? new PaymentIntentResult(PaymentIntentOutcome.Created, existing.Reference,
                $"{PayUrlPrefix}/{existing.Reference}", string.Empty)
            : new PaymentIntentResult(PaymentIntentOutcome.Duplicate, existing.Reference,
                $"{PayUrlPrefix}/{existing.Reference}", string.Empty));
    }

    public Task<PaymentVerification> VerifyAsync(string gatewayReference, CancellationToken cancellationToken)
    {
        // Replays are stable: an already-recorded verification never changes.
        if (_verifications.TryGetValue(gatewayReference, out var recorded))
        {
            return Task.FromResult(recorded);
        }

        var outcome = _scriptedVerifyOutcomes.Count > 0 ? _scriptedVerifyOutcomes.Dequeue() : PaymentVerificationOutcome.Captured;
        var verification = outcome switch
        {
            PaymentVerificationOutcome.Captured => new PaymentVerification(
                outcome, gatewayReference, 100m, "RUB", string.Empty),
            PaymentVerificationOutcome.Failed => new PaymentVerification(
                outcome, gatewayReference, null, null, "payment-declined"),
            _ => new PaymentVerification(
                PaymentVerificationOutcome.Unknown, gatewayReference, null, null, "verification-timeout"),
        };
        _verifications[gatewayReference] = verification;
        return Task.FromResult(verification);
    }
}
