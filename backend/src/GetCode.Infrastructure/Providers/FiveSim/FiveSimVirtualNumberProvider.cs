using System.Collections.Concurrent;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using GetCode.Application.Providers;
using Microsoft.Extensions.Options;

namespace GetCode.Infrastructure.Providers.FiveSim;

/// <summary>
/// M04-002: the first real virtual-number provider adapter (5SIM, current
/// protocol). Vendor DTOs, status strings and endpoint shapes stay inside this
/// folder; everything crossing the port boundary is canonical.
/// <para>
/// Purchase safety: 5SIM has no idempotency key on buy. A timeout or transport
/// failure AFTER the request was sent is surfaced as
/// <see cref="ProviderErrorCode.AmbiguousOutcome"/> — the adapter records the
/// idempotency key so a retry with the same key is refused until reconciliation
/// (M04-006) proves no duplicate purchase happened. This guard is process-local;
/// durable provider-operation state lives in Application/Persistence layers.
/// </para>
/// <para>
/// Redaction: the token lives only in request headers; safe error codes are
/// stable ASCII tokens, never raw provider text; phone numbers appear only in
/// the canonical reservation record which is documented as sensitive.
/// </summary>
public sealed class FiveSimVirtualNumberProvider : IVirtualNumberProvider
{
    public const string ProviderKeyValue = "five-sim";

    private static readonly Dictionary<string, (string Token, ProviderErrorCode Code)> ErrorTextMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // 5SIM returns short plain-text errors; matched case-insensitively.
            ["no free phones"] = ("no-inventory", ProviderErrorCode.OfferUnavailable),
            ["not enough user balance"] = ("insufficient-balance", ProviderErrorCode.InsufficientProviderBalance),
            ["insufficient funds"] = ("insufficient-balance", ProviderErrorCode.InsufficientProviderBalance),
            ["invalid country"] = ("invalid-country", ProviderErrorCode.Rejected),
            ["invalid product"] = ("invalid-service", ProviderErrorCode.Rejected),
            ["invalid operator"] = ("invalid-operator", ProviderErrorCode.Rejected),
            ["reservations not allowed"] = ("rejected", ProviderErrorCode.Rejected),
            ["banned"] = ("rejected", ProviderErrorCode.Rejected),
            ["order not found"] = ("activation-not-found", ProviderErrorCode.Rejected),
            ["bad order id"] = ("activation-not-found", ProviderErrorCode.Rejected),
        };

    private readonly HttpClient _http;
    private readonly FiveSimOptions _options;
    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<string, byte> _ambiguousKeys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProviderReservation> _completedReservations = new(StringComparer.Ordinal);

    public FiveSimVirtualNumberProvider(HttpClient httpClient, IOptions<FiveSimOptions> options, TimeProvider? clock = null)
    {
        _http = httpClient;
        _options = options.Value;
        _clock = clock ?? TimeProvider.System;
    }

    public string ProviderKey => ProviderKeyValue;

    /// <summary>
    /// M04-002 helper for provider balance observation (M04-003 consumes it).
    /// Exposed on the concrete type — deliberately not part of the canonical port.
    /// </summary>
    public async Task<ProviderResult<decimal>> GetBalanceAsync(CancellationToken cancellationToken)
    {
        var scope = await SendAndReadAsync("/v1/user/profile", treatTimeoutAsAmbiguous: false, cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult<decimal>.Failure(error.Code, error.Token);
        }

        var profile = FiveSimWire.Deserialize<FiveSimWire.ProfileDto>(scope.Body!);
        return profile is null
            ? ProviderResult<decimal>.Failure(ProviderErrorCode.InvalidResponse, "malformed-response")
            : ProviderResult<decimal>.Success(profile.Balance);
    }

    public async Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(
        ProviderSearchQuery query, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_options.CountryMap.TryGetValue(query.CountryKey, out var vendorCountry))
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Failure(ProviderErrorCode.Rejected, "invalid-country");
        }

        var url = $"/v1/guest/prices?country={Uri.EscapeDataString(vendorCountry)}&product={Uri.EscapeDataString(query.ServiceKey)}";
        var scope = await SendAndReadAsync(url, treatTimeoutAsAmbiguous: false, cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Failure(error.Code, error.Token);
        }

        var offers = new List<ProviderOffer>();
        foreach (var (op, cost, count) in FiveSimWire.ParsePriceBranch(scope.Json!.Value))
        {
            if (count <= 0)
            {
                continue; // stock exhausted: not an offer at all
            }

            offers.Add(new ProviderOffer(
                ProviderOfferKey: $"{vendorCountry}|{query.ServiceKey}|{op}",
                CostAmount: cost,
                CostCurrency: "RUB",
                IsAvailable: true,
                ObservedAtUtc: _clock.GetUtcNow()));
        }

        return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(offers);
    }

    public async Task<ProviderResult<ProviderReservation>> ReserveAsync(
        ProviderReservationRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        // Duplicate-charge guard: a previously ambiguous key must never re-buy
        // until reconciliation resolves it.
        if (_ambiguousKeys.ContainsKey(request.IdempotencyKey))
        {
            return ProviderResult<ProviderReservation>.Failure(
                ProviderErrorCode.AmbiguousOutcome, "duplicate-purchase-risk");
        }

        if (_completedReservations.TryGetValue(request.IdempotencyKey, out var replay))
        {
            return ProviderResult<ProviderReservation>.Success(replay);
        }

        var segments = request.ProviderOfferKey.Split('|');
        if (segments.Length != 3)
        {
            return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.Rejected, "malformed-offer-key");
        }

        var (vendorCountry, product, op) = (segments[0], segments[1], segments[2]);
        var url = $"/v1/user/buy/activation/{Uri.EscapeDataString(vendorCountry)}/{Uri.EscapeDataString(op)}/{Uri.EscapeDataString(product)}";
        var scope = await SendAndReadAsync(url, treatTimeoutAsAmbiguous: true, cancellationToken);
        if (scope.Error is { } error)
        {
            if (error.IsTransport)
            {
                // The request may have reached 5SIM before failing — outcome unknown.
                _ambiguousKeys.TryAdd(request.IdempotencyKey, 0);
                return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.AmbiguousOutcome, "ambiguous-purchase");
            }

            return ProviderResult<ProviderReservation>.Failure(error.Code, error.Token);
        }

        var order = FiveSimWire.Deserialize<FiveSimWire.OrderDto>(scope.Body!);
        if (order is null || order.Id <= 0 || string.IsNullOrWhiteSpace(order.Phone))
        {
            return ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
        }

        DateTimeOffset? expires = DateTimeOffset.TryParse(order.Expires, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            ? parsed
            : null;
        var reservation = new ProviderReservation(
            ProviderOperationId: order.Id.ToString(CultureInfo.InvariantCulture),
            PhoneNumberE164: order.Phone!,
            ReservedAtUtc: _clock.GetUtcNow(),
            ExpiresAtUtc: expires);
        _completedReservations.TryAdd(request.IdempotencyKey, reservation);
        return ProviderResult<ProviderReservation>.Success(reservation);
    }

    public async Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(
        string providerOperationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync(
            $"/v1/user/check/{Uri.EscapeDataString(providerOperationId)}", treatTimeoutAsAmbiguous: false, cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult<ProviderActivationSnapshot>.Failure(error.Code, error.Token);
        }

        var order = FiveSimWire.Deserialize<FiveSimWire.OrderDto>(scope.Body!);
        if (order is null || order.Id <= 0)
        {
            return ProviderResult<ProviderActivationSnapshot>.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
        }

        return ProviderResult<ProviderActivationSnapshot>.Success(new ProviderActivationSnapshot(
            order.Id.ToString(CultureInfo.InvariantCulture),
            MapStatus(order.Status),
            HasMessage: order.Sms is { Count: > 0 },
            ObservedAtUtc: _clock.GetUtcNow()));
    }

    public async Task<ProviderResult> CancelAsync(string providerOperationId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scope = await SendAndReadAsync(
            $"/v1/user/cancel/{Uri.EscapeDataString(providerOperationId)}", treatTimeoutAsAmbiguous: false, cancellationToken);
        if (scope.Error is { } error)
        {
            return ProviderResult.Failure(error.Code, error.Token);
        }

        // A cancel whose outcome we cannot parse must not be reported as success:
        // the activation might still be charging rent.
        var order = FiveSimWire.Deserialize<FiveSimWire.OrderDto>(scope.Body!);
        if (order is null || !string.Equals(order.Status, "CANCELED", StringComparison.OrdinalIgnoreCase))
        {
            return ProviderResult.Failure(ProviderErrorCode.InvalidResponse, "malformed-response");
        }

        return ProviderResult.Success();
    }

    private static ProviderActivationState MapStatus(string? status) => status?.ToUpperInvariant() switch
    {
        "PENDING" => ProviderActivationState.WaitingForMessage,
        "RECEIVED" => ProviderActivationState.MessageReceived,
        "COMPLETED" => ProviderActivationState.Completed,
        "CANCELED" => ProviderActivationState.Cancelled,
        "TIMEOUT" => ProviderActivationState.Expired,
        "BANNED" => ProviderActivationState.Failed,
        _ => ProviderActivationState.Unknown,
    };

    private async Task<CallScope> SendAndReadAsync(
        string pathAndQuery, bool treatTimeoutAsAmbiguous, CancellationToken cancellationToken)
    {
        HttpResponseMessage response;
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, pathAndQuery);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);
            request.Headers.Accept.ParseAdd("application/json");
            request.Headers.UserAgent.ParseAdd("getcode-platform/1.0");
            response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // HttpClient timeout fired — for purchases this is ambiguity, not absence.
            return CallScope.Transport(treatTimeoutAsAmbiguous, "timeout", ProviderErrorCode.Timeout);
        }
        catch (HttpRequestException)
        {
            return CallScope.Transport(treatTimeoutAsAmbiguous, "transient-http", ProviderErrorCode.Unavailable);
        }
        catch (IOException)
        {
            return CallScope.Transport(treatTimeoutAsAmbiguous, "transient-http", ProviderErrorCode.Unavailable);
        }

        string body;
        try
        {
            body = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException)
        {
            return CallScope.Transport(treatTimeoutAsAmbiguous, "transient-http", ProviderErrorCode.Unavailable);
        }
        catch (IOException)
        {
            return CallScope.Transport(treatTimeoutAsAmbiguous, "transient-http", ProviderErrorCode.Unavailable);
        }

        if (!response.IsSuccessStatusCode)
        {
            var (token, code) = MapError(response.StatusCode, body);
            return CallScope.Protocol(token, code);
        }

        var trimmedBody = body.TrimStart();
        if (!trimmedBody.StartsWith('{') && !trimmedBody.StartsWith('['))
        {
            // 5SIM reports some failures as plain text over HTTP 200
            // (e.g. "no free phones"). Known texts map to canonical tokens;
            // anything else is an unusable response.
            var (textToken, textCode) = MapError(response.StatusCode, body);
            return textCode != ProviderErrorCode.Rejected
                ? CallScope.Protocol(textToken, textCode)
                : CallScope.Protocol("malformed-response", ProviderErrorCode.InvalidResponse);
        }

        try
        {
            using var document = JsonDocument.Parse(body.Trim());
            return CallScope.Success(document.RootElement.Clone(), body);
        }
        catch (JsonException)
        {
            return CallScope.Protocol("malformed-response", ProviderErrorCode.InvalidResponse);
        }
    }

    private static (string Token, ProviderErrorCode Code) MapError(HttpStatusCode statusCode, string rawBody)
    {
        var text = (rawBody ?? string.Empty).Trim().Trim('"');
        foreach (var (needle, mapping) in ErrorTextMap)
        {
            if (text.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                return mapping;
            }
        }

        return statusCode switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => ("auth-failed", ProviderErrorCode.AuthenticationFailed),
            HttpStatusCode.TooManyRequests => ("rate-limited", ProviderErrorCode.RateLimited),
            _ when (int)statusCode >= 500 => ("transient-http", ProviderErrorCode.Unavailable),
            _ => ("rejected", ProviderErrorCode.Rejected),
        };
    }

    private sealed record CallScopeError(string Token, ProviderErrorCode Code, bool IsTransport);

    private sealed class CallScope
    {
        private CallScope(JsonElement? json, string? body, CallScopeError? error)
        {
            Json = json;
            Body = body;
            Error = error;
        }

        public JsonElement? Json { get; }
        public string? Body { get; }
        public CallScopeError? Error { get; }

        public static CallScope Success(JsonElement json, string body) => new(json, body, null);
        public static CallScope Transport(bool ambiguous, string token, ProviderErrorCode code) =>
            new(null, null, new CallScopeError(token, ambiguous ? ProviderErrorCode.AmbiguousOutcome : code, IsTransport: true));
        public static CallScope Protocol(string token, ProviderErrorCode code) =>
            new(null, null, new CallScopeError(token, code, IsTransport: false));
    }
}
