using GetCode.Application.Payments;
using GetCode.Infrastructure.Payments;

namespace GetCode.ProviderContractTests;

/// <summary>
/// M06-003: payment contract tests — the fake gateway must satisfy the same
/// normalized-contract semantics a real gateway adapter will have to: explicit
/// outcomes, idempotent intents, stable verification replays, safe error codes.
/// </summary>
public sealed class FakePaymentGatewayContractTests
{
    private static PaymentIntentRequest NewRequest(string key = "idem-1") => new(
        OrderId: Guid.NewGuid(), Amount: 127m, Currency: "RUB", IdempotencyKey: key, ReturnUrl: null);

    [Fact]
    public async Task Intent_creation_returns_normalized_success_with_reference_and_pay_url()
    {
        var gateway = new FakePaymentGateway();
        var result = await gateway.CreateIntentAsync(NewRequest(), TestContext.Current.CancellationToken);

        Assert.Equal(PaymentIntentOutcome.Created, result.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(result.GatewayReference));
        Assert.StartsWith("https://pay.fake.test/", result.PayUrl);
        Assert.Equal(string.Empty, result.SafeErrorCode); // success carries no error token
    }

    [Fact]
    public async Task Duplicate_intent_key_resolves_to_the_same_gateway_reference()
    {
        var gateway = new FakePaymentGateway();
        var orderId = Guid.NewGuid();
        var first = await gateway.CreateIntentAsync(NewRequest("dup-key") with { OrderId = orderId }, TestContext.Current.CancellationToken);
        var second = await gateway.CreateIntentAsync(NewRequest("dup-key") with { OrderId = orderId }, TestContext.Current.CancellationToken);

        Assert.Equal(first.GatewayReference, second.GatewayReference); // no double intent for one order
    }

    [Fact]
    public async Task Non_positive_amount_is_definitively_rejected_with_safe_token()
    {
        var gateway = new FakePaymentGateway();
        var request = NewRequest() with { Amount = 0m };
        var result = await gateway.CreateIntentAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentIntentOutcome.Rejected, result.Outcome);
        Assert.Null(result.GatewayReference);
        Assert.Equal("invalid-amount", result.SafeErrorCode);
    }

    [Fact]
    public async Task Verification_defaults_to_captured_and_carries_amount()
    {
        var gateway = new FakePaymentGateway();
        var intent = await gateway.CreateIntentAsync(NewRequest("verify-ok"), TestContext.Current.CancellationToken);
        var verification = await gateway.VerifyAsync(intent.GatewayReference!, TestContext.Current.CancellationToken);

        Assert.Equal(PaymentVerificationOutcome.Captured, verification.Outcome);
        Assert.NotNull(verification.CapturedAmount);
        Assert.Equal("RUB", verification.CapturedCurrency);
    }

    [Fact]
    public async Task Scripted_failure_maps_to_definitive_failed_outcome()
    {
        var gateway = new FakePaymentGateway();
        gateway.QueueVerification(PaymentVerificationOutcome.Failed);
        var verification = await gateway.VerifyAsync("fake-ref-failed", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentVerificationOutcome.Failed, verification.Outcome);
        Assert.Null(verification.CapturedAmount);
        Assert.Equal("payment-declined", verification.SafeErrorCode);
    }

    [Fact]
    public async Task Scripted_unknown_marks_ambiguity_for_reconciliation()
    {
        var gateway = new FakePaymentGateway();
        gateway.QueueVerification(PaymentVerificationOutcome.Unknown);
        var verification = await gateway.VerifyAsync("fake-ref-unknown", TestContext.Current.CancellationToken);

        Assert.Equal(PaymentVerificationOutcome.Unknown, verification.Outcome);
        Assert.Null(verification.CapturedAmount); // no money claimed without proof
        Assert.Equal("verification-timeout", verification.SafeErrorCode);
    }

    [Fact]
    public async Task Verification_replays_are_stable_never_flipping_state()
    {
        var gateway = new FakePaymentGateway();
        var first = await gateway.VerifyAsync("fake-ref-replay", TestContext.Current.CancellationToken);
        var replay = await gateway.VerifyAsync("fake-ref-replay", TestContext.Current.CancellationToken);
        var third = await gateway.VerifyAsync("fake-ref-replay", TestContext.Current.CancellationToken);

        Assert.Equal(first, replay);
        Assert.Equal(replay, third);
        Assert.Equal(PaymentVerificationOutcome.Captured, third.Outcome);
    }

    [Fact]
    public void Canonical_contract_types_stay_in_Application()
    {
        // The port + records live in Application; the fake only implements them.
        Assert.True(typeof(IPaymentGateway).Namespace == "GetCode.Application.Payments");
        Assert.True(typeof(FakePaymentGateway).IsAssignableTo(typeof(IPaymentGateway)));
    }
}
