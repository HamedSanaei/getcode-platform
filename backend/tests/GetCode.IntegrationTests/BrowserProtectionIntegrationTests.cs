using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.Application.Identity;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M02-003 verification: CSRF negative tests, origin abuse rejection,
/// credentialed-CORS allow-list behavior, and trusted redirect resolution.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class BrowserProtectionIntegrationTests(DatabaseFixture database)
{
    private const string StrongPassword = "correct-horse-Battery7";
    private const string PrimaryHost = "getcode.example";
    private const string PlusPremiumHost = "vnumber.pluspremium.ir";

    [Fact]
    public async Task State_changing_requests_without_csrf_token_are_rejected()
    {
        using var factory = new GetCodeApiFactory(database);
        var client = CreateHttpsClient(factory);

        // No csrf cookie, no header → antiforgery validation must fail closed.
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.Headers.Host = PrimaryHost;
        request.Headers.Add("Origin", $"https://{PrimaryHost}");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Csrf_flow_allows_legitimate_browser_writes_and_rejects_mismatches()
    {
        using var factory = new GetCodeApiFactory(database);
        var client = CreateHttpsClient(factory);

        // 1. Obtain the token pair (cookie set by server, token echoed by JS).
        var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, csrfResponse.StatusCode);

        var payload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var requestToken = payload.GetProperty("requestToken").GetString();
        Assert.False(string.IsNullOrWhiteSpace(requestToken));
        var setCookie = csrfResponse.Headers.GetValues("Set-Cookie").Single();
        Assert.StartsWith("__Host-xcsrf=", setCookie);
        Assert.Contains("samesite=strict", setCookie, StringComparison.OrdinalIgnoreCase);
        // HttpOnly stays on: the SPA receives the request token in the response
        // body and echoes it via X-XSRF-TOKEN; the cookie only needs to ride along.
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        var cookiePair = setCookie.Split(';')[0];

        // 2. Correct pairing passes (logout is idempotent even without a session).
        var goodRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        goodRequest.Headers.Host = PrimaryHost;
        goodRequest.Headers.Add("Cookie", cookiePair);
        goodRequest.Headers.Add("X-XSRF-TOKEN", requestToken);
        goodRequest.Headers.Add("Origin", $"https://{PrimaryHost}");
        Assert.Equal(HttpStatusCode.NoContent, (await client.SendAsync(goodRequest, TestContext.Current.CancellationToken)).StatusCode);

        // 3. Wrong header value fails closed.
        var badHeader = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        badHeader.Headers.Host = PrimaryHost;
        badHeader.Headers.Add("Cookie", cookiePair);
        badHeader.Headers.Add("X-XSRF-TOKEN", "forged-token-value");
        badHeader.Headers.Add("Origin", $"https://{PrimaryHost}");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(badHeader, TestContext.Current.CancellationToken)).StatusCode);

        // 4. Missing header with cookie present still fails (double-submit broken).
        var missingHeader = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        missingHeader.Headers.Host = PrimaryHost;
        missingHeader.Headers.Add("Cookie", cookiePair);
        missingHeader.Headers.Add("Origin", $"https://{PrimaryHost}");
        Assert.Equal(HttpStatusCode.Forbidden, (await client.SendAsync(missingHeader, TestContext.Current.CancellationToken)).StatusCode);
    }

    [Fact]
    public async Task Cross_site_origin_is_rejected_even_with_valid_token_pair()
    {
        using var factory = new GetCodeApiFactory(database);
        var client = CreateHttpsClient(factory);

        var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        var payload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var requestToken = payload.GetProperty("requestToken").GetString()!;
        var cookiePair = csrfResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        // Attacker page at evil.example forges a same-site form; Origin betrays it.
        var forged = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "victim@example.com", password = "whatever-Pw3!" }),
        };
        forged.Headers.Host = PrimaryHost;
        forged.Headers.Add("Cookie", cookiePair);
        forged.Headers.Add("X-XSRF-TOKEN", requestToken);
        forged.Headers.Add("Origin", "https://evil.example");
        var response = await client.SendAsync(forged, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Credentialed_cors_is_allow_list_only()
    {
        using var allowedFactory = new GetCodeApiFactory(database).WithWebHostBuilder(builder =>
            builder.UseSetting("Cors:AllowedOrigins", "https://app.partner.example,https://getcode.example"));
        allowedFactory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        var allowedClient = allowedFactory.CreateClient();

        // Preflight from an allow-listed origin is granted with credentials.
        var preflight = new HttpRequestMessage(HttpMethod.Options, "/api/auth/session");
        preflight.Headers.Host = PrimaryHost;
        preflight.Headers.Add("Origin", "https://app.partner.example");
        preflight.Headers.Add("Access-Control-Request-Method", "GET");
        var preflightResponse = await allowedClient.SendAsync(preflight, TestContext.Current.CancellationToken);
        var aco = string.Join(",", preflightResponse.Headers.GetValues("Access-Control-Allow-Origin"));
        Assert.True(preflightResponse.IsSuccessStatusCode || preflightResponse.StatusCode == HttpStatusCode.NoContent);
        Assert.Equal("https://app.partner.example", aco.Trim());
        Assert.Contains("true", preflightResponse.Headers.TryGetValues("Access-Control-Allow-Credentials", out var creds) ? creds : Enumerable.Empty<string>());

        // Preflight from a non-listed origin gets no ACAO grant.
        var denied = new HttpRequestMessage(HttpMethod.Options, "/api/auth/session");
        denied.Headers.Host = PrimaryHost;
        denied.Headers.Add("Origin", "https://evil.example");
        denied.Headers.Add("Access-Control-Request-Method", "GET");
        var deniedResponse = await allowedClient.SendAsync(denied, TestContext.Current.CancellationToken);
        Assert.False(deniedResponse.Headers.Contains("Access-Control-Allow-Origin"));

        // Default deployment (no Cors config): nothing is granted cross-origin.
        var defaultFactory = new GetCodeApiFactory(database);
        defaultFactory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        using var defaultClient = defaultFactory.CreateClient();
        var defaultPreflight = new HttpRequestMessage(HttpMethod.Options, "/api/catalog/countries");
        defaultPreflight.Headers.Host = PrimaryHost;
        defaultPreflight.Headers.Add("Origin", "https://anyone.example");
        defaultPreflight.Headers.Add("Access-Control-Request-Method", "GET");
        var defaultResponse = await defaultClient.SendAsync(defaultPreflight, TestContext.Current.CancellationToken);
        Assert.False(defaultResponse.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Redirect_targets_are_selected_from_the_site_allow_list()
    {
        using var factory = new GetCodeApiFactory(database);
        using var scope = factory.Services.CreateScope();
        var resolver = scope.ServiceProvider.GetRequiredService<GetCode.Application.SiteHosts.TrustedRedirectResolver>();
        // Site context is request-scoped middleware state; the resolver takes it as input.
        var site = new GetCode.Application.SiteHosts.SiteDescriptor(
            "primary", PrimaryHost, new Uri("https://getcode.example"), "getcode", IsCanonical: true);

        // Relative path on the current site is kept and absolutized.
        Assert.Equal("https://getcode.example/orders", resolver.ResolveReturnUrl("/orders", site));

        // Empty/missing falls back to the current site base.
        Assert.Equal("https://getcode.example", resolver.ResolveReturnUrl(null, site));

        // Absolute URL on the OTHER configured site is allowed (allow-listed).
        Assert.Equal(
            "https://vnumber.pluspremium.ir/wallet",
            resolver.ResolveReturnUrl("https://vnumber.pluspremium.ir/wallet", site));

        // Foreign absolute origin collapses to the current site base.
        Assert.Equal("https://getcode.example", resolver.ResolveReturnUrl("https://evil.example/phish", site));

        // Scheme-relative and backslash tricks collapse too.
        Assert.Equal("https://getcode.example", resolver.ResolveReturnUrl("//evil.example/x", site));
        Assert.Equal("https://getcode.example", resolver.ResolveReturnUrl("/\\evil.example", site));

        // HTTP downgrade of an allowed host is refused (scheme must match https).
        Assert.Equal("https://getcode.example", resolver.ResolveReturnUrl("http://vnumber.pluspremium.ir/wallet", site));
        scope.Dispose();
    }

    /// <summary>HTTPS base address so SecurePolicy.Always cookies behave like production.</summary>
    private static HttpClient CreateHttpsClient(GetCodeApiFactory factory)
    {
        factory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        return factory.CreateClient();
    }
}
