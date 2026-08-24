using System.Text.Json;
using System.Text.Json.Serialization;
using GetCode.Application.Providers;
using Microsoft.Extensions.Options;

namespace GetCode.Infrastructure.Providers.SecondVendor;

public sealed class SecondVendorOptions
{
    public const string SectionName = "SecondVendor";
    public bool Enabled { get; set; }
    public string BaseUrl { get; set; } = "https://api.secondvendor.test";
    public string ApiToken { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 15;
}

/// <summary>Second-vendor wire models — internal, never escape this folder.</summary>
internal static class SvWire
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    internal sealed record SvOrder(
        [property: JsonPropertyName("order_id")] string? OrderId,
        [property: JsonPropertyName("msisdn")] string? Msisdn,
        [property: JsonPropertyName("state")] string? State,
        [property: JsonPropertyName("messages")] IReadOnlyList<SvMessage>? Messages);

    internal sealed record SvMessage([property: JsonPropertyName("body")] string? Body);

    internal static T? Deserialize<T>(string json) where T : class => JsonSerializer.Deserialize<T>(json, JsonOptions);
}

/// <summary>
/// M04-007: SECOND virtual-number adapter proving the abstraction — a
/// deliberately different wire contract (string order ids, `state`, `messages`,
/// POST-ish semantics simulated over GET for stubbing simplicity) mapped onto
/// the SAME canonical port. No Order/Wallet/Catalog code changes were required
/// to add it. Not a decided production vendor; live use requires a product
/// decision plus credentials.
/// </summary>
public sealed class SecondVendorVirtualNumberProvider : IVirtualNumberProvider
{
    public const string ProviderKeyValue = "second-vendor";

    private readonly HttpClient _http;
    private readonly SecondVendorOptions _options;
    private readonly TimeProvider _clock;

    public SecondVendorVirtualNumberProvider(HttpClient httpClient, IOptions<SecondVendorOptions> options, TimeProvider? clock = null)
    {
        _http = httpClient;
        _options = options.Value;
        _clock = clock ?? TimeProvider.System;
    }

    public string ProviderKey => ProviderKeyValue;

    public async Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(
        ProviderSearchQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync(
            $"/offers/{Uri.EscapeDataString(query.CountryKey)}/{Uri.EscapeDataString(query.ServiceKey)}", cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Failure(error.Code, error.Token);
        }

        var offers = new List<ProviderOffer>();
        if (scope.Json is { } root && root.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in root.EnumerateArray())
            {
                var key = entry.TryGetProperty("offer_id", out var k) ? k.GetString() : null;
                var price = entry.TryGetProperty("price", out var p) && p.ValueKind == JsonValueKind.Number ? p.GetDecimal() : -1m;
                var available = entry.TryGetProperty("available", out var a) && a.ValueKind == JsonValueKind.True;
                if (key is null || !available)
                {
                    continue;
                }

                offers.Add(new ProviderOffer($"{ProviderKeyValue}:{key}", Math.Max(price, 0m), "USD", true, _clock.GetUtcNow()));
            }
        }

        return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(offers);
    }

    public async Task<ProviderResult<ProviderReservation>> ReserveAsync(
        ProviderReservationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync(
            $"/reserve/{Uri.EscapeDataString(request.ProviderOfferKey)}?idempotency={Uri.EscapeDataString(request.IdempotencyKey)}",
            cancellationToken);
        if (scope.Error is { } error)
        {
            // Transport-level faults leave the purchase state unknown →
            // ambiguous; vendor-answered failures are definitive refusals.
            return error.Code is ProviderErrorCode.Timeout or ProviderErrorCode.Unavailable
                ? ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase")
                : ProviderResult<ProviderReservation>.Failure(error.Code, error.Token);
        }

        var order = SvWire.Deserialize<SvWire.SvOrder>(scope.Body!);
        if (order?.OrderId is null || order.Msisdn is null || !order.Msisdn.StartsWith('+'))
        {
            return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
        }

        return ProviderResult<ProviderReservation>.Success(
            new ProviderReservation(order.OrderId, order.Msisdn, _clock.GetUtcNow(), _clock.GetUtcNow().AddHours(1)));
    }

    public async Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(
        string providerOperationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync($"/orders/{Uri.EscapeDataString(providerOperationId)}", cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult<ProviderActivationSnapshot>.Failure(error.Code, error.Token);
        }

        var order = SvWire.Deserialize<SvWire.SvOrder>(scope.Body!);
        if (order?.OrderId is null)
        {
            return ProviderResult<ProviderActivationSnapshot>.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
        }

        var state = order.State switch
        {
            "WAITING" => ProviderActivationState.WaitingForMessage,
            "GOT_SMS" => ProviderActivationState.MessageReceived,
            "DONE" => ProviderActivationState.Completed,
            "CANCELLED" => ProviderActivationState.Cancelled,
            "EXPIRED" => ProviderActivationState.Expired,
            _ => ProviderActivationState.Unknown,
        };
        return ProviderResult<ProviderActivationSnapshot>.Success(
            new ProviderActivationSnapshot(order.OrderId, state, HasMessage: order.Messages is { Count: > 0 }, _clock.GetUtcNow()));
    }

    public async Task<ProviderResult> CancelAsync(string providerOperationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync($"/orders/{Uri.EscapeDataString(providerOperationId)}/cancel", cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult.Failure(error.Code, error.Token);
        }

        var order = SvWire.Deserialize<SvWire.SvOrder>(scope.Body!);
        return order?.State == "CANCELLED"
            ? ProviderResult.Success()
            : ProviderResult.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
    }

    private async Task<(JsonElement? Json, string? Body, (string Token, ProviderErrorCode Code)? Error)> SendAndReadAsync(
        string path, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _options.ApiToken);
            request.Headers.UserAgent.ParseAdd("getcode-platform/1.0");
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, null, ("timeout", ProviderErrorCode.Timeout));
        }
        catch (HttpRequestException)
        {
            return (null, null, ("transient-http", ProviderErrorCode.Unavailable));
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var token = (int)response.StatusCode switch
            {
                401 or 403 => "auth-failed",
                429 => "rate-limited",
                >= 500 => "transient-http",
                _ => "rejected",
            };
            var code = token switch
            {
                "auth-failed" => ProviderErrorCode.AuthenticationFailed,
                "rate-limited" => ProviderErrorCode.RateLimited,
                "transient-http" => ProviderErrorCode.Unavailable,
                _ => ProviderErrorCode.Rejected,
            };
            return (null, null, (token, code));
        }

        try
        {
            using var document = JsonDocument.Parse(body.Trim());
            return (document.RootElement.Clone(), body, null);
        }
        catch (JsonException)
        {
            return (null, null, ("malformed-response", ProviderErrorCode.InvalidResponse));
        }
    }
}
