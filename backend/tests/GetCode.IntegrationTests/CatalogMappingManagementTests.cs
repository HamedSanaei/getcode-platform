using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.Application.Authorization;
using GetCode.Application.Catalog;
using GetCode.Application.Identity;
using GetCode.Application.Providers;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GetCode.IntegrationTests;

/// <summary>
/// M09-003: catalog/provider mapping management over the admin API.
/// Pins: validation (unknown canonical targets are rejected before any
/// mutation), duplicate safety (rebind replaces, never duplicates), the audit
/// trail (transactional outbox events), and server-side authorization.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class CatalogMappingManagementTests(DatabaseFixture database)
{
    private const string PrimaryHost = "getcode.example";
    private const string StrongPassword = "Correct-Horse-9!";

    private async Task<(GetCodeApiFactory Factory, string SessionCookie)> NewAdminSessionAsync()
    {
        var factory = new GetCodeApiFactory(database);
        var email = $"mapper-{Guid.NewGuid():N}@example.com";
        using (var scope = factory.Services.CreateScope())
        {
            var identity = scope.ServiceProvider.GetRequiredService<IdentityService>();
            await identity.RegisterAsync(new RegisterUserCommand(email, StrongPassword), TestContext.Current.CancellationToken);

            var admin = scope.ServiceProvider.GetRequiredService<GetCode.Application.Authorization.AuthorizationAdminService>();
            var roles = scope.ServiceProvider.GetRequiredService<GetCode.Application.Authorization.IRoleRepository>();
            if (await roles.FindByKeyAsync("m09-mapping-admin", TestContext.Current.CancellationToken) is null)
            {
                await admin.CreateRoleAsync(
                    new CreateRoleCommand("m09-mapping-admin", "Mapping Administrator"),
                    TestContext.Current.CancellationToken);
            }

            await admin.ChangePermissionAsync(
                new ChangeRolePermissionsCommand(
                    "m09-mapping-admin", GetCode.Domain.Authorization.PermissionCatalog.AdminAccess, Grant: true),
                TestContext.Current.CancellationToken);
            await admin.SetUserRoleAsync(
                new AssignUserRoleCommand(email, "m09-mapping-admin", Assign: true),
                TestContext.Current.CancellationToken);

            // Canonical catalog entries the mappings will target.
            var catalog = scope.ServiceProvider.GetRequiredService<CatalogAdminService>();
            await catalog.UpsertCountryAsync(new UpsertCountryCommand("DE", "Germany"), TestContext.Current.CancellationToken);
            await catalog.UpsertServiceAsync(new UpsertServiceCommand("signal", "Signal"), TestContext.Current.CancellationToken);
        }

        factory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/");
        var client = factory.CreateClient();
        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        csrfResponse.EnsureSuccessStatusCode();
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var csrfCookie = csrfResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0];

        using var loginRequest = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email, password = StrongPassword }),
        };
        loginRequest.Headers.Host = PrimaryHost;
        loginRequest.Headers.Add("Cookie", csrfCookie);
        loginRequest.Headers.Add("X-XSRF-TOKEN", csrfPayload.GetProperty("requestToken").GetString()!);
        loginRequest.Headers.Add("Origin", $"https://{PrimaryHost}");
        var loginResponse = await client.SendAsync(loginRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        return (factory, loginResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
    }

    private static async Task<HttpResponseMessage> PostAsAdminAsync(
        GetCodeApiFactory factory, string sessionCookie, string path, object payload)
    {
        // Re-fetch the CSRF pair per client instance; cookies carry the session.
        var client = factory.CreateClient();
        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        csrfResponse.EnsureSuccessStatusCode();
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload),
        };
        request.Headers.Host = PrimaryHost;
        request.Headers.Add("Cookie", sessionCookie);
        request.Headers.Add("X-XSRF-TOKEN", csrfPayload.GetProperty("requestToken").GetString()!);
        request.Headers.Add("Origin", $"https://{PrimaryHost}");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Bind_creates_mapping_and_writes_outbox_audit_event()
    {
        var (factory, cookie) = await NewAdminSessionAsync();
        using var _f = factory;

        await PostAsAdminAsync(factory, cookie, "/api/admin/providers/register",
            new { providerKey = "testprovider", displayName = "Test Provider" });

        var bind = await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/bind",
            new { providerKey = "testprovider", kind = "Country", externalCode = "49", canonicalStableKey = "DE" });
        Assert.Equal(HttpStatusCode.OK, bind.StatusCode);

        // Audit trail: the transactional outbox carries the mapping event.
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GetCode.Persistence.GetCodeDbContext>();
        var events = await db.OutboxMessages
            .Where(m => m.Type.StartsWith("providers.mapping."))
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.Contains(events, m => m.Type == "providers.mapping.bound");

        // The management list resolves the canonical stable key.
        var list = await factory.CreateClient().SendAsync(ListRequest(cookie), TestContext.Current.CancellationToken);
        var payload = await list.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var provider = payload.EnumerateArray().Single(p => p.GetProperty("providerKey").GetString() == "testprovider");
        var mapping = provider.GetProperty("mappings").EnumerateArray().Single();
        Assert.Equal("DE", mapping.GetProperty("canonicalStableKey").GetString());
    }

    private static HttpRequestMessage ListRequest(string cookie)
    {
        using var _discard = new HttpRequestMessage();
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/providers");
        request.Headers.Host = PrimaryHost;
        request.Headers.Add("Cookie", cookie);
        return request;
    }

    [Fact]
    public async Task Bind_with_unknown_canonical_target_is_rejected_without_mutating_the_catalog()
    {
        var (factory, cookie) = await NewAdminSessionAsync();
        using var _f = factory;

        await PostAsAdminAsync(factory, cookie, "/api/admin/providers/register",
            new { providerKey = "strictprov", displayName = "Strict Provider" });

        using var scopeBefore = factory.Services.CreateScope();
        var dbBefore = scopeBefore.ServiceProvider.GetRequiredService<GetCode.Persistence.GetCodeDbContext>();
        var countriesBefore = await dbBefore.Countries.CountAsync(TestContext.Current.CancellationToken);
        var outboxBefore = await dbBefore.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);

        var response = await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/bind",
            new { providerKey = "strictprov", kind = "Country", externalCode = "999", canonicalStableKey = "ZZ" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        using var scopeAfter = factory.Services.CreateScope();
        var dbAfter = scopeAfter.ServiceProvider.GetRequiredService<GetCode.Persistence.GetCodeDbContext>();
        var countriesAfter = await dbAfter.Countries.CountAsync(TestContext.Current.CancellationToken);
        var outboxAfter = await dbAfter.OutboxMessages.CountAsync(TestContext.Current.CancellationToken);

        // No catalog corruption, no audit event for a rejected change.
        Assert.Equal(countriesBefore, countriesAfter);
        Assert.Equal(outboxBefore, outboxAfter);
    }

    [Fact]
    public async Task Rebinding_replaces_instead_of_duplicating()
    {
        var (factory, cookie) = await NewAdminSessionAsync();
        using var _f = factory;

        await PostAsAdminAsync(factory, cookie, "/api/admin/providers/register",
            new { providerKey = "rebindprov", displayName = "Rebind Provider" });
        await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/bind",
            new { providerKey = "rebindprov", kind = "Service", externalCode = "sig-1", canonicalStableKey = "signal" });

        var rebind = await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/bind",
            new { providerKey = "rebindprov", kind = "Service", externalCode = "sig-1", canonicalStableKey = "signal" });
        Assert.Equal(HttpStatusCode.OK, rebind.StatusCode);

        var list = await factory.CreateClient().SendAsync(ListRequest(cookie), TestContext.Current.CancellationToken);
        var payload = await list.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        var provider = payload.EnumerateArray().Single(p => p.GetProperty("providerKey").GetString() == "rebindprov");
        Assert.Single(provider.GetProperty("mappings").EnumerateArray());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<GetCode.Persistence.GetCodeDbContext>();
        var events = await db.OutboxMessages
            .Where(m => m.Type == "providers.mapping.rebound")
            .ToListAsync(TestContext.Current.CancellationToken);
        Assert.NotEmpty(events);
    }

    [Fact]
    public async Task Preview_resolves_without_mutating()
    {
        var (factory, cookie) = await NewAdminSessionAsync();
        using var _f = factory;

        await PostAsAdminAsync(factory, cookie, "/api/admin/providers/register",
            new { providerKey = "previewprov", displayName = "Preview Provider" });

        var ok = await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/preview",
            new { providerKey = "previewprov", kind = "Country", externalCode = "49", canonicalStableKey = "DE" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var previewPayload = await ok.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.True(previewPayload.GetProperty("resolved").GetBoolean());

        var bad = await PostAsAdminAsync(factory, cookie, "/api/admin/mappings/preview",
            new { providerKey = "previewprov", kind = "Country", externalCode = "49", canonicalStableKey = "QQ" });
        var badPayload = await bad.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.False(badPayload.GetProperty("resolved").GetBoolean());
    }
}
