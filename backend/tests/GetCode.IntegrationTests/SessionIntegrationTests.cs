using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using GetCode.Application.Identity;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M02-002 verification: browser-session flows over HTTP against two
/// configured hostnames sharing one identity database. Cookies are handled
/// explicitly (Set-Cookie parsing → Cookie header) so attributes and host
/// scoping are asserted rather than hidden by a cookie container.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class SessionIntegrationTests(DatabaseFixture database)
{
    private const string StrongPassword = "correct-horse-Battery7";
    private const string PrimaryHost = "getcode.example";
    private const string PlusPremiumHost = "vnumber.pluspremium.ir";

    [Fact]
    public async Task Login_issues_host_scoped_cookie_and_session_round_trips()
    {
        using var factory = new GetCodeApiFactory(database);
        var email = $"session-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(factory, email);

        // Login on the primary host.
        var (response, cookie) = await LoginAsync(factory, PrimaryHost, email);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Cookie contract: __Host- prefix (Secure + Path=/ + no Domain enforced
        // by browsers), HttpOnly, SameSite=Lax, 7-day Max-Age.
        Assert.StartsWith("__Host-gc_session=", cookie.RawHeader);
        Assert.True(cookie.RawHeader.Contains("HttpOnly", StringComparison.OrdinalIgnoreCase), $"actual set-cookie: {cookie.RawHeader}");
        Assert.True(cookie.RawHeader.Contains("Secure", StringComparison.OrdinalIgnoreCase), $"actual set-cookie: {cookie.RawHeader}");
        Assert.True(cookie.RawHeader.Contains("SameSite=Lax", StringComparison.OrdinalIgnoreCase), $"actual set-cookie: {cookie.RawHeader}");
        Assert.DoesNotContain("domain=", cookie.RawHeader, StringComparison.OrdinalIgnoreCase);
        // ~7 days = 604800 seconds (integer truncation may yield 604799).
        Assert.Contains("max-age=6047", cookie.RawHeader.Replace(" ", string.Empty), StringComparison.OrdinalIgnoreCase);
        Assert.True(cookie.Value.Length >= 40); // ≥256 bits of entropy base64url-encoded

        // Presenting the cookie authenticates the session on the same host.
        var (meStatus, me) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", cookie.RawHeader);
        Assert.Equal(HttpStatusCode.OK, meStatus);
        using var doc = JsonDocument.Parse(await me.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.NotEqual(Guid.Empty, doc.RootElement.GetProperty("userId").GetGuid());
    }

    [Fact]
    public async Task Same_identity_gets_independent_sessions_on_both_hosts()
    {
        using var factory = new GetCodeApiFactory(database);
        var email = $"two-hosts-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(factory, email);

        var (_, primaryCookie) = await LoginAsync(factory, PrimaryHost, email);
        var (_, premiumCookie) = await LoginAsync(factory, PlusPremiumHost, email);

        // Distinct cookie names per site; no shared parent-domain scope.
        Assert.StartsWith("__Host-gc_session=", primaryCookie.Pair);
        Assert.StartsWith("__Host-vpp_session=", premiumCookie.Pair);
        Assert.NotEqual(primaryCookie.Value, premiumCookie.Value);

        // Each session authenticates only on its own host…
        var (_, primaryMe) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", primaryCookie.Pair);
        Assert.Equal(HttpStatusCode.OK, primaryMe.StatusCode);
        var (_, premiumMe) = await GetAsyncWithCookie(factory, PlusPremiumHost, "/api/auth/session", premiumCookie.Pair);
        Assert.Equal(HttpStatusCode.OK, premiumMe.StatusCode);

        // …and raw token replay across hosts is refused server-side.
        var (_, crossSite) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", premiumCookie.Pair);
        Assert.Equal(HttpStatusCode.Unauthorized, crossSite.StatusCode);
        var (_, crossSite2) = await GetAsyncWithCookie(factory, PlusPremiumHost, "/api/auth/session", primaryCookie.Pair);
        Assert.Equal(HttpStatusCode.Unauthorized, crossSite2.StatusCode);
    }

    [Fact]
    public async Task Logout_revokes_server_side_so_replayed_cookies_die()
    {
        using var factory = new GetCodeApiFactory(database);
        var email = $"logout-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(factory, email);

        var (_, cookie) = await LoginAsync(factory, PrimaryHost, email);
        Assert.Equal(HttpStatusCode.OK, (await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", cookie.RawHeader)).Item1);

        // Logout with the session cookie + CSRF contract.
        var logout = await PostWithSessionAsync(factory, PrimaryHost, "/api/auth/logout", cookie.Pair);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        // The captured token no longer works — revocation is server-side state.
        var (status, _) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", cookie.RawHeader);
        Assert.Equal(HttpStatusCode.Unauthorized, status);
    }

    [Fact]
    public async Task Rotation_replaces_the_token_without_touching_other_sessions()
    {
        using var factory = new GetCodeApiFactory(database);
        var scope = factory.Services.CreateScope();
        var sessions = scope.ServiceProvider.GetRequiredService<SessionService>();
        var email = $"rotate-{Guid.NewGuid():N}@example.com";
        await RegisterAsync(factory, email);

        var (_, firstDevice) = await LoginAsync(factory, PrimaryHost, email);
        var (_, secondDevice) = await LoginAsync(factory, PrimaryHost, email);

        // Rotate the second device's session via the endpoint (CSRF contract).
        var rotatedResponse = await PostWithSessionAsync(factory, PrimaryHost, "/api/auth/session/rotate", secondDevice.Pair);
        Assert.Equal(HttpStatusCode.OK, rotatedResponse.StatusCode);

        // Old token dead; replacement works; the untouched first device still works.
        var (oldStatus, _) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", secondDevice.Pair);
        Assert.Equal(HttpStatusCode.Unauthorized, oldStatus);

        var setCookie = rotatedResponse.Headers.GetValues("Set-Cookie").Single();
        Assert.StartsWith("__Host-gc_session=", setCookie);
        var newValue = setCookie.Split(';')[0].Split('=', 2)[1];
        var validation = await sessions.ValidateAsync(newValue, "primary", TestContext.Current.CancellationToken);
        Assert.IsType<SessionValidationResult.Success>(validation);

        var (firstStatus, _) = await GetAsyncWithCookie(factory, PrimaryHost, "/api/auth/session", firstDevice.Pair);
        Assert.Equal(HttpStatusCode.OK, firstStatus);
        scope.Dispose();
    }

    [Fact]
    public async Task Expired_session_is_rejected()
    {
        using var factory = new GetCodeApiFactory(database);
        Guid userId;
        Guid sessionId;
        string token;
        using (var scope = factory.Services.CreateScope())
        {
            var sessions = scope.ServiceProvider.GetRequiredService<SessionService>();
            var users = scope.ServiceProvider.GetRequiredService<IdentityService>();
            var email = $"expiry-{Guid.NewGuid():N}@example.com";
            var registered = await users.RegisterAsync(new RegisterUserCommand(email, StrongPassword), TestContext.Current.CancellationToken);
            userId = registered.UserId;
            var issued = await sessions.IssueAsync(userId, "primary", TestContext.Current.CancellationToken);
            sessionId = issued.SessionId;
            token = issued.Token;
            Assert.IsType<SessionValidationResult.Success>(await sessions.ValidateAsync(token, "primary", TestContext.Current.CancellationToken));
        }

        // Force expiry directly in the store: absolute lifetimes are checked against now.
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<Persistence.GetCodeDbContext>()
            .UseNpgsql(database.ConnectionString).Options;
        await using (var context = new Persistence.GetCodeDbContext(options))
        {
            var row = await context.Sessions.SingleAsync(s => s.Id == sessionId, TestContext.Current.CancellationToken);
            row.GetType().GetProperty("ExpiresAtUtc")!.SetValue(row, DateTimeOffset.UtcNow.AddMinutes(-1));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Fresh scope: the earlier validation must not leave a stale tracked instance.
        using var verifier = factory.Services.CreateScope();
        var validator = verifier.ServiceProvider.GetRequiredService<SessionService>();
        Assert.IsType<SessionValidationResult.Expired>(await validator.ValidateAsync(token, "primary", TestContext.Current.CancellationToken));
    }

    private static async Task RegisterAsync(GetCodeApiFactory factory, string email)
    {
        using var scope = factory.Services.CreateScope();
        var identity = scope.ServiceProvider.GetRequiredService<IdentityService>();
        await identity.RegisterAsync(new RegisterUserCommand(email, StrongPassword), TestContext.Current.CancellationToken);
    }

    /// <summary>HTTPS base address so SecurePolicy.Always cookies behave like production.</summary>
    private static HttpClient CreateHttpsClient(GetCodeApiFactory factory)
    {
        factory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        return factory.CreateClient();
    }

    /// <summary>M02-003 browser contract: fetch the CSRF pair before any state-changing call.</summary>
    private static async Task<(string CookiePair, string RequestToken)> GetCsrfPairAsync(
        HttpClient client, string host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        request.Headers.Host = host;
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var cookiePair = response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
        return (cookiePair, payload.GetProperty("requestToken").GetString()!);
    }

    private static async Task<(HttpResponseMessage Response, SessionCookie Cookie)> LoginAsync(
        GetCodeApiFactory factory, string host, string email)
    {
        var client = CreateHttpsClient(factory);
        var (csrfCookie, csrfToken) = await GetCsrfPairAsync(client, host);
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = StrongPassword }),
        };
        request.Headers.Host = host;
        request.Headers.Add("Cookie", csrfCookie);
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        request.Headers.Add("Origin", $"https://{host}");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            return (response, new SessionCookie(string.Empty, string.Empty, string.Empty));
        }

        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        var pair = setCookie.Split(';')[0];
        var name = pair.Split('=', 2)[0];
        var value = pair.Split('=', 2)[1];
        return (response, new SessionCookie(setCookie, $"{name}={value}", value));
    }

    private static async Task<HttpResponseMessage> PostWithSessionAsync(
        GetCodeApiFactory factory, string host, string path, string sessionCookieHeader)
    {
        var client = CreateHttpsClient(factory);
        var (csrfCookie, csrfToken) = await GetCsrfPairAsync(client, host);
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Headers.Host = host;
        request.Headers.Add("Cookie", $"{sessionCookieHeader.Split(';')[0]}; {csrfCookie}");
        request.Headers.Add("X-XSRF-TOKEN", csrfToken);
        request.Headers.Add("Origin", $"https://{host}");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async Task<(HttpStatusCode Status, HttpResponseMessage Message)> GetAsyncWithCookie(
        GetCodeApiFactory factory, string host, string path, string cookieHeader)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = host;
        request.Headers.Add("Cookie", cookieHeader.Split(';')[0]);
        var response = await client.SendAsync(request);
        return (response.StatusCode, response);
    }

    /// <summary>RawHeader keeps every Set-Cookie attribute; Pair is the transport form.</summary>
    private sealed record SessionCookie(string RawHeader, string Pair, string Value);
}
