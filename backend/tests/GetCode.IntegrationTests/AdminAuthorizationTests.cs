using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GetCode.IntegrationTests;

/// <summary>
/// M09-001: admin authorization contract.
///
/// The security boundary is server-side: /api/auth/principal requires a valid
/// session, and every /api/admin/* route requires the canonical admin.access
/// capability via the permission policy. The SPA's capability view (roles +
/// permissions) is navigation UX only — these tests pin the enforcement.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class AdminAuthorizationTests(DatabaseFixture database)
{
    private const string PrimaryHost = "getcode.example";
    private const string StrongPassword = "Correct-Horse-9!";

    private async Task<GetCodeApiFactory> NewFactoryAsync(string email, bool grantAdmin)
    {
        var factory = new GetCodeApiFactory(database);
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<GetCode.Application.Identity.IdentityService>();
            await identity.RegisterAsync(
                new GetCode.Application.Identity.RegisterUserCommand(email, StrongPassword),
                TestContext.Current.CancellationToken);

            if (grantAdmin)
            {
                var admin = scope.ServiceProvider.GetRequiredService<GetCode.Application.Authorization.AuthorizationAdminService>();
                // The database fixture is shared across the collection; other tests
                // may have created this system role already.
                var roles = scope.ServiceProvider.GetRequiredService<GetCode.Application.Authorization.IRoleRepository>();
                if (await roles.FindByKeyAsync("m09-shell-admin", TestContext.Current.CancellationToken) is null)
                {
                    await admin.CreateRoleAsync(
                        new GetCode.Application.Authorization.CreateRoleCommand("m09-shell-admin", "Platform Administrator"),
                        TestContext.Current.CancellationToken);
                }

                await admin.ChangePermissionAsync(
                    new GetCode.Application.Authorization.ChangeRolePermissionsCommand(
                        "m09-shell-admin", GetCode.Domain.Authorization.PermissionCatalog.AdminAccess, Grant: true),
                    TestContext.Current.CancellationToken);
                await admin.SetUserRoleAsync(
                    new GetCode.Application.Authorization.AssignUserRoleCommand(email, "m09-shell-admin", Assign: true),
                    TestContext.Current.CancellationToken);
            }
        }

        factory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        return factory;
    }

    /// <summary>Logs in over the browser contract and returns the session cookie pair.</summary>
    private static async Task<string> LoginAndGetSessionCookieAsync(GetCodeApiFactory factory, string email)
    {
        var client = factory.CreateClient();
        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        csrfResponse.EnsureSuccessStatusCode();
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var csrfCookie = csrfResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = StrongPassword }),
        };
        request.Headers.Host = PrimaryHost;
        request.Headers.Add("Cookie", csrfCookie);
        request.Headers.Add("X-XSRF-TOKEN", csrfPayload.GetProperty("requestToken").GetString()!);
        request.Headers.Add("Origin", $"https://{PrimaryHost}");
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
    }

    private static async Task<JsonElement> GetPrincipalAsync(GetCodeApiFactory factory, string sessionCookie)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/principal");
        request.Headers.Host = PrimaryHost;
        request.Headers.Add("Cookie", sessionCookie);
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
    }

    private static async Task<HttpStatusCode> GetStatusAsync(GetCodeApiFactory factory, string path, string? sessionCookie)
    {
        var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Host = PrimaryHost;
        if (sessionCookie is not null)
        {
            request.Headers.Add("Cookie", sessionCookie);
        }

        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        return response.StatusCode;
    }

    [Fact]
    public async Task Principal_requires_a_valid_session()
    {
        using var factory = await NewFactoryAsync($"anon-{Guid.NewGuid():N}@example.com", grantAdmin: false);
        Assert.Equal(HttpStatusCode.Unauthorized, await GetStatusAsync(factory, "/api/auth/principal", null));
    }

    [Fact]
    public async Task Authenticated_user_gets_stable_capability_view_without_any_permission()
    {
        var email = $"plain-{Guid.NewGuid():N}@example.com";
        using var factory = await NewFactoryAsync(email, grantAdmin: false);
        var sessionCookie = await LoginAndGetSessionCookieAsync(factory, email);

        var principal = await GetPrincipalAsync(factory, sessionCookie);
        Assert.NotEqual(Guid.Empty, Guid.Parse(principal.GetProperty("userId").GetString()!));
        // Deny-by-default: a fresh user holds no roles and no capabilities at all.
        Assert.Empty(principal.GetProperty("roles").EnumerateArray());
        Assert.Empty(principal.GetProperty("permissions").EnumerateArray());

        // Direct admin API access without the capability fails even with a valid session:
        // frontend guards are UX only; this policy is the boundary.
        Assert.Equal(HttpStatusCode.Forbidden, await GetStatusAsync(factory, "/api/admin/overview", sessionCookie));
    }

    [Fact]
    public async Task Admin_capable_principal_exposes_role_and_canonical_permissions_and_reaches_admin_api()
    {
        var email = $"admin-{Guid.NewGuid():N}@example.com";
        using var factory = await NewFactoryAsync(email, grantAdmin: true);
        var sessionCookie = await LoginAndGetSessionCookieAsync(factory, email);

        var principal = await GetPrincipalAsync(factory, sessionCookie);
        Assert.Contains("m09-shell-admin", principal.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
        Assert.Contains(
            GetCode.Domain.Authorization.PermissionCatalog.AdminAccess,
            principal.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()));

        Assert.Equal(HttpStatusCode.OK, await GetStatusAsync(factory, "/api/admin/overview", sessionCookie));

        // Unauthenticated callers are rejected outright on the same API.
        Assert.Equal(HttpStatusCode.Unauthorized, await GetStatusAsync(factory, "/api/admin/overview", null));
    }
}
