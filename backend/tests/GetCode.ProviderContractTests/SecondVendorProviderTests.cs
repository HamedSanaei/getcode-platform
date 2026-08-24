using System.Net;
using System.Text;
using GetCode.Application.Providers;
using GetCode.Infrastructure.Providers.SecondVendor;
using Microsoft.Extensions.Options;

namespace GetCode.ProviderContractTests;

/// <summary>
/// M04-007: the SECOND concrete adapter must satisfy exactly the same
/// behavioral contract as FiveSim and the fake — that is the abstraction proof.
/// </summary>
public sealed class SecondVendorProviderTests : VirtualNumberProviderContractTests
{
    protected override string OfferKey => "sv-offer-1";

    protected override Task<IVirtualNumberProvider> CreateProviderAsync()
    {
        var httpClient = new HttpClient(new StubHandler()) { BaseAddress = new Uri("https://api.secondvendor.test") };
        return Task.FromResult<IVirtualNumberProvider>(new SecondVendorVirtualNumberProvider(
            httpClient, Options.Create(new SecondVendorOptions { Enabled = true, ApiToken = "test-token" })));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private int _nextOrderId = 88000100;
        private readonly Dictionary<string, (string OrderId, string Msisdn, string State)> _byIdempotency = new(StringComparer.Ordinal);
        private readonly Dictionary<string, (string OrderId, string Msisdn, string State)> _byOrderId = new(StringComparer.Ordinal);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            var idempotency = request.RequestUri.Query.Replace("?idempotency=", string.Empty, StringComparison.Ordinal);
            var auth = request.Headers.Authorization?.ToString();
            if (auth != "Bearer test-token")
            {
                return Respond(HttpStatusCode.Unauthorized, """{"error":"unauthorized"}""");
            }

            if (path.StartsWith("/offers/", StringComparison.Ordinal))
            {
                var country = path.Split('/')[2];
                if (country == "XX")
                {
                    return Respond(HttpStatusCode.NotFound, """{"error":"unknown_country"}""");
                }

                return Respond(HttpStatusCode.OK,
                    """[{"offer_id":"sv-offer-1","price":9.99,"available":true},{"offer_id":"sv-offer-2","price":12.5,"available":false}]""");
            }

            if (path.StartsWith("/reserve/", StringComparison.Ordinal))
            {
                if (_byIdempotency.TryGetValue(idempotency, out var existing))
                {
                    return Respond(HttpStatusCode.OK, OrderJson(existing));
                }

                var orderId = $"sv-{_nextOrderId++}";
                var msisdn = $"+790012{_nextOrderId:D5}";
                var order = (OrderId: orderId, Msisdn: msisdn, State: "WAITING");
                _byIdempotency[idempotency] = order;
                _byOrderId[orderId] = order;
                return Respond(HttpStatusCode.OK, OrderJson(order));
            }

            if (path.StartsWith("/orders/", StringComparison.Ordinal) && path.EndsWith("/cancel", StringComparison.Ordinal))
            {
                var orderId = path.Split('/')[2];
                if (!_byOrderId.TryGetValue(orderId, out var order))
                {
                    return Respond(HttpStatusCode.NotFound, """{"error":"not_found"}""");
                }

                var cancelled = order with { State = "CANCELLED" };
                _byOrderId[orderId] = cancelled;
                foreach (var (key, value) in _byIdempotency.ToArray())
                {
                    if (value.OrderId == orderId)
                    {
                        _byIdempotency[key] = cancelled;
                    }
                }

                return Respond(HttpStatusCode.OK, OrderJson(cancelled));
            }

            if (path.StartsWith("/orders/", StringComparison.Ordinal))
            {
                var orderId = path.Split('/')[2];
                return _byOrderId.TryGetValue(orderId, out var order)
                    ? Respond(HttpStatusCode.OK, OrderJson(order.State == "WAITING" ? order with { State = "GOT_SMS" } : order))
                    : Respond(HttpStatusCode.NotFound, """{"error":"not_found"}""");
            }

            return Respond(HttpStatusCode.NotFound, """{"error":"no_route"}""");
        }

        private static string OrderJson((string OrderId, string Msisdn, string State) order) =>
            $$"""{"order_id":"{{order.OrderId}}","msisdn":"{{order.Msisdn}}","state":"{{order.State}}","messages":[]}""";

        private static Task<HttpResponseMessage> Respond(HttpStatusCode status, string json)
        {
            var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
            return Task.FromResult(response);
        }
    }
}
