using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using GetCode.Application.Providers;
using GetCode.Infrastructure.Providers.FiveSim;
using Microsoft.Extensions.Options;

namespace GetCode.ProviderContractTests;

/// <summary>
/// M04-002: runs the shared virtual-number provider contract suite against the
/// real 5SIM adapter wired to a stateful stubbed HTTP handler — no credentials,
/// no live balance, fully deterministic. Dedicated tests below pin the vendor
/// failure mapping, ambiguous-purchase safety and redaction guarantees.
/// </summary>
public sealed class FiveSimProviderTests : VirtualNumberProviderContractTests
{
    protected override string OfferKey => "germany|telegram|any";

    protected override Task<IVirtualNumberProvider> CreateProviderAsync() =>
        Task.FromResult<IVirtualNumberProvider>(CreateProvider());

    internal static FiveSimVirtualNumberProvider CreateProvider(Action<StubHandler>? configure = null)
    {
        var handler = new StubHandler();
        configure?.Invoke(handler);
        return Build(handler);
    }

    /// <summary>Creates the adapter plus the live stub handle for stateful scenarios.</summary>
    internal static FiveSimVirtualNumberProvider CreateProvider(out StubHandler handler)
    {
        handler = new StubHandler();
        return Build(handler);
    }

    private static FiveSimVirtualNumberProvider Build(StubHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://5sim.net") },
            Options.Create(new FiveSimOptions
            {
                Enabled = true,
                ApiToken = "secret-token-for-tests",
                BaseUrl = "https://5sim.net",
                CountryMap = { ["IR"] = "iran", ["DE"] = "germany" },
            }));

    private static ProviderReservationRequest NewReservation(string key) =>
        new("germany|telegram|any", key, "corr");

    /// <summary>
    /// Deterministic, minimally stateful in-process HTTP stub. Successful buys
    /// allocate increasing order ids; check/cancel resolve only known ids
    /// (mirroring 5SIM's "order not found" behavior) — enough statefulness for
    /// the shared contract flows without modeling a real backend.
    /// </summary>
    internal sealed class StubHandler : HttpMessageHandler
    {
        public string PricesJson { get; set; } =
            """{"iran":{"telegram":{"any":{"cost":7.5,"count":42,"rate":91},"mtt":{"cost":8.0,"count":3,"rate":80}}}}""";

        public HttpStatusCode BuyStatus { get; set; } = HttpStatusCode.OK;
        public string BuyBody { get; set; } = string.Empty;
        public string CheckStatus { get; set; } = "PENDING";
        public int SmsCount { get; set; }
        public bool LockAllToUnauthorized { get; set; }
        public Exception? ThrowOnNextBuy { get; set; }

        public HttpRequestMessage? LastRequest { get; private set; }

        private long _nextOrderId = 555000110;
        private long? _purchasedOrderId;

        private static string OrderJson(long id, string status, int smsCount)
        {
            var sms = smsCount > 0
                ? ""","sms":[{"id":1,"code":"1234","text":"Your code","created":"2026-08-24T12:00:00Z"}]"""
                : ""","sms":[]""";
            var expires = DateTime.UtcNow.AddMinutes(20).ToString("yyyy-MM-dd'T'HH:mm:ss.ffffffzzz", CultureInfo.InvariantCulture);
            return $$"""{"id":{{id}},"phone":"+79001234567","operator":"any","product":"telegram","price":7.5,"status":"{{status}}","expires":"{{expires}}"{{sms}}}""";
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            await Task.Yield();
            var path = request.RequestUri!.PathAndQuery;

            if (LockAllToUnauthorized)
            {
                return Text(HttpStatusCode.Unauthorized, "unauthorized");
            }

            if (path.StartsWith("/v1/guest/prices", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, PricesJson);
            }

            if (path.StartsWith("/v1/user/buy/", StringComparison.Ordinal))
            {
                if (ThrowOnNextBuy is not null)
                {
                    ThrowOnNextBuy = null;
                    throw new TaskCanceledException("simulated timeout/transport failure after send");
                }

                var body = BuyBody.Length > 0 ? BuyBody : OrderJson(++_nextOrderId, "PENDING", 0);
                if (body.TrimStart().StartsWith('{'))
                {
                    using var document = JsonDocument.Parse(body);
                    if (document.RootElement.TryGetProperty("id", out var idElement) && idElement.ValueKind == JsonValueKind.Number)
                    {
                        _purchasedOrderId = idElement.GetInt64();
                    }
                }

                return Json(BuyStatus, body);
            }

            if (path.StartsWith("/v1/user/check/", StringComparison.Ordinal) || path.StartsWith("/v1/user/cancel/", StringComparison.Ordinal))
            {
                var rawId = path.Split('/')[^1];
                if (_purchasedOrderId is { } known && rawId == known.ToString(CultureInfo.InvariantCulture))
                {
                    if (path.StartsWith("/v1/user/cancel/", StringComparison.Ordinal))
                    {
                        CheckStatus = "CANCELED";
                        return Json(HttpStatusCode.OK, OrderJson(known, "CANCELED", 0));
                    }

                    return Json(HttpStatusCode.OK, OrderJson(known, CheckStatus, SmsCount));
                }

                return Text(HttpStatusCode.NotFound, "order not found");
            }

            if (path.StartsWith("/v1/user/profile", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"id":42,"email":"ops@example.com","balance":150.25}""");
            }

            return Text(HttpStatusCode.NotFound, "unknown route");
        }

        private static HttpResponseMessage Json(HttpStatusCode status, string body) => Respond(status, body, "application/json");

        private static HttpResponseMessage Text(HttpStatusCode status, string body) => Respond(status, body, "text/plain");

        private static HttpResponseMessage Respond(HttpStatusCode status, string body, string media) =>
            new(status) { Content = new StringContent(body, Encoding.UTF8, media) };
    }

    // ---- Shared-suite adaptations ---------------------------------------------

    [Fact]
    public async Task Purchase_failure_matrix_maps_to_canonical_safe_tokens()
    {
        var cases = new (string Body, HttpStatusCode Status, ProviderErrorCode Code, string Token)[]
        {
            ("no free phones", HttpStatusCode.OK, ProviderErrorCode.OfferUnavailable, "no-inventory"),
            ("not enough user balance", HttpStatusCode.OK, ProviderErrorCode.InsufficientProviderBalance, "insufficient-balance"),
            ("invalid country", HttpStatusCode.BadRequest, ProviderErrorCode.Rejected, "invalid-country"),
            ("invalid product", HttpStatusCode.BadRequest, ProviderErrorCode.Rejected, "invalid-service"),
            ("invalid operator", HttpStatusCode.BadRequest, ProviderErrorCode.Rejected, "invalid-operator"),
            ("reservations not allowed", HttpStatusCode.Forbidden, ProviderErrorCode.Rejected, "rejected"),
            (string.Empty, HttpStatusCode.Unauthorized, ProviderErrorCode.AuthenticationFailed, "auth-failed"),
            (string.Empty, HttpStatusCode.TooManyRequests, ProviderErrorCode.RateLimited, "rate-limited"),
            (string.Empty, HttpStatusCode.BadGateway, ProviderErrorCode.Unavailable, "transient-http"),
        };

        foreach (var (body, status, expectedCode, expectedToken) in cases)
        {
            var provider = CreateProvider(h =>
            {
                h.BuyStatus = status;
                h.BuyBody = body;
            });
            var result = await provider.ReserveAsync(NewReservation($"map-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

            Assert.False(result.IsSuccess);
            Assert.Equal(expectedCode, result.ErrorCode);
            Assert.Equal(expectedToken, result.SafeErrorCode);
        }
    }

    [Fact]
    public async Task Malformed_buy_body_maps_to_invalid_response()
    {
        var provider = CreateProvider(h =>
        {
            h.BuyStatus = HttpStatusCode.OK;
            h.BuyBody = "<html>not json</html>";
        });

        var result = await provider.ReserveAsync(NewReservation($"bad-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProviderErrorCode.InvalidResponse, result.ErrorCode);
        Assert.Equal("malformed-response", result.SafeErrorCode);
    }

    [Fact]
    public async Task Timeout_during_purchase_is_ambiguous_and_blocks_same_key_retry()
    {
        var provider = CreateProvider(h => h.ThrowOnNextBuy = new TaskCanceledException("simulated"));

        var first = await provider.ReserveAsync(NewReservation("amb-1"), TestContext.Current.CancellationToken);
        Assert.False(first.IsSuccess);
        Assert.Equal(ProviderErrorCode.AmbiguousOutcome, first.ErrorCode);
        Assert.Equal("ambiguous-purchase", first.SafeErrorCode);

        // The SAME idempotency key must never silently re-buy.
        var retry = await provider.ReserveAsync(NewReservation("amb-1"), TestContext.Current.CancellationToken);
        Assert.False(retry.IsSuccess);
        Assert.Equal(ProviderErrorCode.AmbiguousOutcome, retry.ErrorCode);
        Assert.Equal("duplicate-purchase-risk", retry.SafeErrorCode);

        // A different key proceeds normally once transport heals.
        var healed = await provider.ReserveAsync(NewReservation("amb-2"), TestContext.Current.CancellationToken);
        Assert.True(healed.IsSuccess);
    }

    [Fact]
    public async Task Transport_failure_during_purchase_is_also_ambiguous()
    {
        var provider = CreateProvider(h => h.ThrowOnNextBuy = new HttpRequestException("connection reset mid-flight"));

        var result = await provider.ReserveAsync(NewReservation("conn-1"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProviderErrorCode.AmbiguousOutcome, result.ErrorCode);
    }

    [Fact]
    public async Task Activation_status_mapping_covers_all_protocol_states()
    {
        var cases = new (string Status, ProviderActivationState Expected, bool HasSms)[]
        {
            ("PENDING", ProviderActivationState.WaitingForMessage, false),
            ("RECEIVED", ProviderActivationState.MessageReceived, true),
            ("COMPLETED", ProviderActivationState.Completed, false),
            ("CANCELED", ProviderActivationState.Cancelled, false),
            ("TIMEOUT", ProviderActivationState.Expired, false),
            ("BANNED", ProviderActivationState.Failed, false),
        };

        foreach (var (status, expectedState, hasSms) in cases)
        {
            var provider = CreateProvider(out var stub);
            var reserved = await provider.ReserveAsync(NewReservation($"st-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
            Assert.True(reserved.IsSuccess);

            stub.CheckStatus = status;
            stub.SmsCount = hasSms ? 1 : 0;
            var snapshot = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);

            Assert.True(snapshot.IsSuccess);
            Assert.Equal(expectedState, snapshot.Value!.State);
            Assert.Equal(hasSms, snapshot.Value!.HasMessage);
        }
    }

    [Fact]
    public async Task Repeated_sms_polls_keep_has_message_true_and_state_received()
    {
        var provider = CreateProvider(out var stub);
        var reserved = await provider.ReserveAsync(NewReservation($"rep-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        Assert.True(reserved.IsSuccess);
        stub.CheckStatus = "RECEIVED";
        stub.SmsCount = 2;

        var first = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);
        var second = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);

        Assert.True(first.Value!.HasMessage);
        Assert.True(second.Value!.HasMessage);
        Assert.Equal(ProviderActivationState.MessageReceived, second.Value!.State);
    }

    [Fact]
    public async Task Balance_observation_returns_provider_balance_without_leaking_email()
    {
        var provider = CreateProvider();

        var balance = await ((FiveSimVirtualNumberProvider)provider).GetBalanceAsync(TestContext.Current.CancellationToken);

        Assert.True(balance.IsSuccess);
        Assert.Equal(150.25m, balance.Value);
        Assert.DoesNotContain("ops@example.com", JsonSerializer.Serialize(balance));
    }

    [Fact]
    public async Task Auth_failure_on_check_maps_to_authentication_failed()
    {
        var provider = CreateProvider(out var stub);
        var reserved = await provider.ReserveAsync(NewReservation($"auth-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        Assert.True(reserved.IsSuccess);

        stub.LockAllToUnauthorized = true;
        var denied = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);

        Assert.False(denied.IsSuccess);
        Assert.Equal(ProviderErrorCode.AuthenticationFailed, denied.ErrorCode);
        Assert.Equal("auth-failed", denied.SafeErrorCode);
    }

    [Fact]
    public async Task Unknown_country_key_fails_fast_with_invalid_country_token()
    {
        var provider = CreateProvider();

        var result = await provider.SearchOffersAsync(new ProviderSearchQuery("ZZ", "telegram", "activation"), TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.Equal(ProviderErrorCode.Rejected, result.ErrorCode);
        Assert.Equal("invalid-country", result.SafeErrorCode);
    }

    [Fact]
    public async Task Bearer_token_never_appears_in_urls_or_canonical_results()
    {
        var provider = CreateProvider(out var stub);

        await provider.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), TestContext.Current.CancellationToken);
        var reserved = await provider.ReserveAsync(NewReservation($"redact-{Guid.NewGuid():N}"), TestContext.Current.CancellationToken);
        Assert.True(reserved.IsSuccess);

        var serialized = JsonSerializer.Serialize(reserved.Value);
        Assert.DoesNotContain("secret-token-for-tests", serialized, StringComparison.OrdinalIgnoreCase);

        var uri = stub.LastRequest!.RequestUri!.ToString();
        Assert.DoesNotContain("secret-token-for-tests", uri, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("Bearer secret-token-for-tests", stub.LastRequest.Headers.Authorization!.ToString());
    }
}
