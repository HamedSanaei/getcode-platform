using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GetCode.Application.Notifications;
using Microsoft.Extensions.Options;

namespace GetCode.Infrastructure.Notifications.Sms.Kavenegar;

/// <summary>
/// M04-008: configuration for the Kavenegar outbound-SMS adapter. The API key,
/// sender line and verification template name are secrets/configuration — never
/// source. Enabled=false keeps the sender unregistered.
/// </summary>
public sealed class KavenegarOptions
{
    public const string SectionName = "Kavenegar";

    public bool Enabled { get; set; }

    /// <summary>Kavenegar API key (secret). Never logged.</summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Sender line for plain transactional SMS.</summary>
    public string? Sender { get; set; }

    /// <summary>Approved VerifyLookup template name for verification codes.</summary>
    public string VerificationTemplate { get; set; } = string.Empty;

    /// <summary>Official API origin; override only for stubbed tests.</summary>
    public string BaseUrl { get; set; } = "https://api.kavenegar.com";

    public int TimeoutSeconds { get; set; } = 15;
}

/// <summary>
/// M04-008: first real outbound-SMS adapter (Kavenegar REST). Verification codes
/// go through the official templated VerifyLookup flow; ordinary notifications
/// through sms/send. The adapter classifies outcomes into canonical results with
/// stable safe tokens and a transient-retryable flag — retry policy itself
/// belongs to the dispatch layer (outbox worker, M06-005+), never here.
/// <para>Redaction: the API key lives only in the URL path segment of requests
/// and is stripped from anything surfaced outward; OTP values are never echoed
/// into results or exceptions.</para>
/// </summary>
public sealed class KavenegarSmsNotificationSender : ISmsNotificationPort
{
    private readonly HttpClient _http;
    private readonly KavenegarOptions _options;
    private readonly TimeProvider _clock;

    public KavenegarSmsNotificationSender(HttpClient httpClient, IOptions<KavenegarOptions> options, TimeProvider? clock = null)
    {
        _http = httpClient;
        _options = options.Value;
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<SmsDeliveryResult> SendVerificationCodeAsync(
        VerificationCodeSmsRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var receptor = IranianMobileNumber.Normalize(request.RecipientE164);
        if (receptor is null || string.IsNullOrEmpty(_options.VerificationTemplate))
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.Rejected, "invalid-request", retryable: false);
        }

        var url = $"/v1/{Uri.EscapeDataString(_options.ApiKey)}/verify/lookup.json" +
                  $"?receptor={Uri.EscapeDataString(receptor)}" +
                  $"&token={Uri.EscapeDataString(request.Code)}" +
                  $"&template={Uri.EscapeDataString(_options.VerificationTemplate)}";
        return await SendAsync(url, cancellationToken);
    }

    public async Task<SmsDeliveryResult> SendTransactionalSmsAsync(
        TransactionalSmsRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var receptor = IranianMobileNumber.Normalize(request.RecipientE164);
        if (receptor is null || string.IsNullOrWhiteSpace(request.MessageText))
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.Rejected, "invalid-request", retryable: false);
        }

        var url = $"/v1/{Uri.EscapeDataString(_options.ApiKey)}/sms/send.json" +
                  $"?receptor={Uri.EscapeDataString(receptor)}" +
                  $"&message={Uri.EscapeDataString(request.MessageText)}" +
                  (_options.Sender is { Length: > 0 } sender ? $"&sender={Uri.EscapeDataString(sender)}" : string.Empty);
        return await SendAsync(url, cancellationToken);
    }

    private async Task<SmsDeliveryResult> SendAsync(string pathAndQuery, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var httpRequest = new HttpRequestMessage(HttpMethod.Get, pathAndQuery);
            httpRequest.Headers.Accept.ParseAdd("application/json");
            httpRequest.Headers.UserAgent.ParseAdd("getcode-platform/1.0");
            response = await _http.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.Timeout, "timeout", retryable: true);
        }
        catch (HttpRequestException)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.ProviderUnavailable, "transient-http", retryable: true);
        }
        catch (IOException)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.ProviderUnavailable, "transient-http", retryable: true);
        }

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.ProviderUnavailable, "transient-http", retryable: true);
        }
        catch (IOException)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.ProviderUnavailable, "transient-http", retryable: true);
        }

        if ((int)response.StatusCode >= 500)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.ProviderUnavailable, "provider-unavailable", retryable: true);
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body.Trim());
        }
        catch (JsonException)
        {
            return SmsDeliveryResult.Failure(SmsDeliveryOutcome.Unknown, "unknown-response", retryable: false);
        }

        using (document)
        {
            var root = document.RootElement;
            var status = root.TryGetProperty("return", out var ret) && ret.TryGetProperty("status", out var st)
                ? st.GetInt32()
                : (int)response.StatusCode;

            long? messageId = null;
            if (root.TryGetProperty("entries", out var entries) && entries.ValueKind == JsonValueKind.Array && entries.GetArrayLength() > 0
                && entries[0].TryGetProperty("messageid", out var mid) && mid.TryGetInt64(out var parsedId))
            {
                messageId = parsedId;
            }

            if (status == 200)
            {
                return SmsDeliveryResult.Accepted(messageId);
            }

            // Kavenegar returns its own status family in `return.status`.
            // Exact vendor code list should be revisited at live verification;
            // the mapping below covers the documented families defensively.
            return status switch
            {
                401 or 403 => SmsDeliveryResult.Failure(SmsDeliveryOutcome.AuthenticationFailed, "auth-failed", retryable: false),
                418 or 24 => SmsDeliveryResult.Failure(SmsDeliveryOutcome.InvalidRecipient, "invalid-recipient", retryable: false),
                420 or 421 or 26 or 27 => SmsDeliveryResult.Failure(SmsDeliveryOutcome.InvalidTemplate, "invalid-template", retryable: false),
                74 or 75 or 76 or 77 or 78 => SmsDeliveryResult.Failure(SmsDeliveryOutcome.RateLimited, "rate-limited", retryable: true),
                429 => SmsDeliveryResult.Failure(SmsDeliveryOutcome.RateLimited, "rate-limited", retryable: true),
                _ => SmsDeliveryResult.Failure(SmsDeliveryOutcome.Rejected, "rejected", retryable: false),
            };
        }
    }
}
