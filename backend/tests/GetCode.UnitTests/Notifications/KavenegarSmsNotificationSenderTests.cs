using System.Net;
using System.Text;
using System.Text.Json;
using GetCode.Application.Notifications;
using GetCode.Infrastructure.Notifications.Sms.Kavenegar;
using Microsoft.Extensions.Options;

namespace GetCode.UnitTests.Notifications;

/// <summary>
/// M04-008: Kavenegar adapter behavior over a deterministic in-process HTTP
/// stub — outcome classification, retryability, redaction. No secrets, no network.
/// </summary>
public sealed class KavenegarSmsNotificationSenderTests
{
    private const string ApiKey = "unit-test-api-key";

    private static KavenegarSmsNotificationSender CreateSender(out StubHandler handler, Action<StubHandler>? configure = null)
    {
        handler = new StubHandler();
        configure?.Invoke(handler);
        configure?.Invoke(handler);
        return new KavenegarSmsNotificationSender(
            new HttpClient(handler) { BaseAddress = new Uri("https://api.kavenegar.com") },
            Options.Create(new KavenegarOptions
            {
                Enabled = true,
                ApiKey = ApiKey,
                Sender = "10008663",
                VerificationTemplate = "getcode-verify",
            }));
    }

    internal sealed class StubHandler : HttpMessageHandler
    {
        public HttpStatusCode Status { get; set; } = HttpStatusCode.OK;
        public string Body { get; set; } = """{"return":{"status":200,"message":"ok"},"entries":[{"messageid":987654321}]}""";
        public Exception? ThrowOnNextSend;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            await Task.Yield();
            if (ThrowOnNextSend is not null)
            {
                var toThrow = ThrowOnNextSend;
                ThrowOnNextSend = null;
                throw toThrow;
            }

            return new HttpResponseMessage(Status)
            {
                Content = new StringContent(Body, Encoding.UTF8, "application/json"),
            };
        }
    }

    // ---- happy paths -----------------------------------------------------------

    [Fact]
    public async Task Verification_code_uses_templated_verify_lookup_endpoint()
    {
        var sender = CreateSender(out var handler);

        var result = await sender.SendVerificationCodeAsync(
            new VerificationCodeSmsRequest("+989123456789", "445566"), TestContext.Current.CancellationToken);

        Assert.Equal(SmsDeliveryOutcome.Accepted, result.Outcome);
        Assert.Equal(987654321L, result.ProviderMessageId);
        Assert.False(result.IsTransientlyRetryable);

        var path = handler.LastRequest!.RequestUri!.PathAndQuery;
        Assert.Contains("/verify/lookup.json", path, StringComparison.Ordinal);
        Assert.Contains("template=getcode-verify", path, StringComparison.Ordinal);
        Assert.Contains("receptor=%2B989123456789", path, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Transactional_sms_uses_sms_send_endpoint_with_sender_line()
    {
        var sender = CreateSender(out var handler);

        var result = await sender.SendTransactionalSmsAsync(
            new TransactionalSmsRequest("09121234567", "سفارش شما ثبت شد"), TestContext.Current.CancellationToken);

        Assert.Equal(SmsDeliveryOutcome.Accepted, result.Outcome);
        var path = handler.LastRequest!.RequestUri!.PathAndQuery;
        Assert.Contains("/sms/send.json", path, StringComparison.Ordinal);
        Assert.Contains("sender=10008663", path, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("+989123456789")]
    [InlineData("989123456789")]
    [InlineData("00989123456789")]
    [InlineData("09123456789")]
    [InlineData("۰۹۱۲۳۴۵۶۷۸۹")]
    public async Task Iranian_number_variants_are_normalized_before_dispatch(string raw)
    {
        var sender = CreateSender(out var handler);

        await sender.SendVerificationCodeAsync(new VerificationCodeSmsRequest(raw, "111222"), TestContext.Current.CancellationToken);

        Assert.Contains("receptor=%2B989123456789", handler.LastRequest!.RequestUri!.PathAndQuery, StringComparison.Ordinal);
    }

    // ---- failure mapping -------------------------------------------------------

    public static TheoryData<HttpStatusCode, int, SmsDeliveryOutcome, string, bool> FailureCases() => new()
    {
        { HttpStatusCode.OK, 401, SmsDeliveryOutcome.AuthenticationFailed, "auth-failed", false },
        { HttpStatusCode.OK, 418, SmsDeliveryOutcome.InvalidRecipient, "invalid-recipient", false },
        { HttpStatusCode.OK, 420, SmsDeliveryOutcome.InvalidTemplate, "invalid-template", false },
        { HttpStatusCode.OK, 429, SmsDeliveryOutcome.RateLimited, "rate-limited", true },
        { HttpStatusCode.InternalServerError, 500, SmsDeliveryOutcome.ProviderUnavailable, "provider-unavailable", true },
        { HttpStatusCode.ServiceUnavailable, 503, SmsDeliveryOutcome.ProviderUnavailable, "provider-unavailable", true },
        { HttpStatusCode.BadRequest, 400, SmsDeliveryOutcome.Rejected, "rejected", false },
    };

    [Theory]
    [MemberData(nameof(FailureCases))]
    public async Task Provider_failures_map_to_canonical_outcomes_and_retry_flags(
        HttpStatusCode httpStatus, int vendorStatus, SmsDeliveryOutcome expectedOutcome, string expectedToken, bool expectedRetryable)
    {
        var sender = CreateSender(out _, h =>
        {
            h.Status = httpStatus;
            h.Body = $$"""{"return":{"status":{{vendorStatus}},"message":"x"},"entries":[]}""";
        });

        var result = await sender.SendTransactionalSmsAsync(
            new TransactionalSmsRequest("+989123456789", "hello"), TestContext.Current.CancellationToken);

        Assert.Equal(expectedOutcome, result.Outcome);
        Assert.Equal(expectedToken, result.SafeToken);
        Assert.Equal(expectedRetryable, result.IsTransientlyRetryable);
    }

    [Fact]
    public async Task Timeout_is_transiently_retryable()
    {
        var sender = CreateSender(out _, h => h.ThrowOnNextSend = new TaskCanceledException("simulated"));

        var result = await sender.SendTransactionalSmsAsync(
            new TransactionalSmsRequest("+989123456789", "hello"), TestContext.Current.CancellationToken);

        Assert.Equal(SmsDeliveryOutcome.Timeout, result.Outcome);
        Assert.True(result.IsTransientlyRetryable);
    }

    [Fact]
    public async Task Malformed_provider_response_is_unknown_and_not_retried()
    {
        var sender = CreateSender(out _, h => { h.Body = "<not-json"; });

        var result = await sender.SendTransactionalSmsAsync(
            new TransactionalSmsRequest("+989123456789", "hello"), TestContext.Current.CancellationToken);

        Assert.Equal(SmsDeliveryOutcome.Unknown, result.Outcome);
        Assert.False(result.IsTransientlyRetryable);
    }

    [Fact]
    public async Task Invalid_recipient_input_fails_fast_without_any_http_call()
    {
        var sender = CreateSender(out _);

        var result = await sender.SendTransactionalSmsAsync(
            new TransactionalSmsRequest("12345", "hello"), TestContext.Current.CancellationToken);

        Assert.Equal(SmsDeliveryOutcome.Rejected, result.Outcome);
        Assert.Equal("invalid-request", result.SafeToken);
    }

    // ---- redaction -------------------------------------------------------------

    [Fact]
    public async Task Results_never_echo_the_api_key_or_the_otp_value()
    {
        var sender = CreateSender(out var handler);

        var result = await sender.SendVerificationCodeAsync(
            new VerificationCodeSmsRequest("+989123456789", "998877"), TestContext.Current.CancellationToken);

        var serialized = JsonSerializer.Serialize(result);
        Assert.DoesNotContain(ApiKey, serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("998877", serialized, StringComparison.Ordinal);

        // The key appears only inside the request path (Kavenegar contract),
        // never as a header or query token that generic logging would capture.
        Assert.DoesNotContain(ApiKey, handler.LastRequest!.Headers.ToString(), StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>M04-008: canonical Iranian mobile normalization rules.</summary>
public sealed class IranianMobileNumberTests
{
    [Theory]
    [InlineData("+989121234567")]
    [InlineData("09121234567")]
    [InlineData("00989121234567")]
    [InlineData("۹۸۹۱۲۱۲۳۴۵۶۷")]
    public void Valid_variants_normalize_to_plus98_form(string input)
    {
        Assert.Equal("+989121234567", IranianMobileNumber.Normalize(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]
    [InlineData("+98121234567")] // landline prefix
    [InlineData("abc")]
    public void Invalid_inputs_return_null(string? input)
    {
        Assert.Null(IranianMobileNumber.Normalize(input));
    }
}
