using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.Application.Identity;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M08-002 groundwork: authenticated checkout API — server-authoritative,
/// duplicate-submit safe (same key → same order), explicit quote failures.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class CheckoutApiTests(DatabaseFixture database)
{
    private const string Host = "getcode.example";

    [Fact]
    public async Task Duplicate_submits_reuse_the_same_order()
    {
        await using var factory = new GetCodeApiFactory(database);
        factory.ClientOptions.BaseAddress = new Uri($"https://{Host}/");
        var email = $"chk-{Guid.NewGuid():N}@example.com";
        using (var scope = factory.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<IdentityService>()
                .RegisterAsync(new RegisterUserCommand(email, "Str0ng!Passw0rd"), TestContext.Current.CancellationToken);
        }

        var client = factory.CreateClient();
        var (csrfCookie, csrfToken) = await CsrfAsync(factory);
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = "Str0ng!Passw0rd" }),
        };
        login.Headers.Host = Host;
        login.Headers.Add("Cookie", csrfCookie);
        login.Headers.Add("X-XSRF-TOKEN", csrfToken);
        login.Headers.Add("Origin", $"https://{Host}");
        Assert.Equal(HttpStatusCode.OK, (await client.SendAsync(login, TestContext.Current.CancellationToken)).StatusCode);

        var (quoteCookie, quoteToken) = await CsrfAsync(factory);
        using var issue = new HttpRequestMessage(HttpMethod.Post, "/api/quotes")
        {
            Content = JsonContent.Create(new { CountryKey = "RU", ServiceKey = "telegram", ProductTypeKey = "activation", ProviderKey = "fake", ProviderCostAmount = 100m, CostCurrency = "RUB" }),
        };
        issue.Headers.Host = Host;
        issue.Headers.Add("Cookie", quoteCookie);
        issue.Headers.Add("X-XSRF-TOKEN", quoteToken);
        issue.Headers.Add("Origin", $"https://{Host}");
        var issued = await client.SendAsync(issue, TestContext.Current.CancellationToken);
        var quote = await issued.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        // Checkout itself: session cookie + fresh CSRF pair.
        var (payCookie, payToken) = await CsrfAsync(factory);
        var orderId = Guid.Empty;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            using var post = new HttpRequestMessage(HttpMethod.Post, "/api/checkout")
            {
                Content = JsonContent.Create(new { QuoteId = quote.GetProperty("quoteId").GetGuid(), ExpectedAmount = 127m, IdempotencyKey = "submit-intent-1" }),
            };
            post.Headers.Host = Host;
            post.Headers.Add("Cookie", payCookie);
            post.Headers.Add("X-XSRF-TOKEN", payToken);
            post.Headers.Add("Origin", $"https://{Host}");
            var response = await client.SendAsync(post, TestContext.Current.CancellationToken);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
            if (attempt == 0)
            {
                Assert.False(body.GetProperty("replayed").GetBoolean());
                orderId = body.GetProperty("orderId").GetGuid();
                continue;
            }

            Assert.True(HttpStatusCode.OK == response.StatusCode, $"status {response.StatusCode}");
            Assert.Equal(orderId, body.GetProperty("orderId").GetGuid());   // same order
            Assert.True(body.GetProperty("replayed").GetBoolean());
        }
    }

    private static async Task<(string Cookie, string Token)> CsrfAsync(GetCodeApiFactory factory)
    {
        using var freshClient = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        request.Headers.Host = Host;
        var response = await freshClient.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        return (response.Headers.GetValues("Set-Cookie").Single().Split(';')[0], payload.GetProperty("requestToken").GetString()!);
    }
}
