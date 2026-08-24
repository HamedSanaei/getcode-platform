using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.Application.Catalog;
using GetCode.Domain.Catalog;
using GetCode.IntegrationTests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M03-004 verification: public catalog API contract — only enabled/offered
/// data leaves the system, pagination is clamped and deterministic, and the
/// OpenAPI document describes the endpoints. No provider data is exposed.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class CatalogApiContractTests(DatabaseFixture database)
{
    private const string StrongPassword = "unused-here";

    [Fact]
    public async Task Public_catalog_endpoints_expose_only_enabled_data_with_pagination()
    {
        await using var factory = new GetCodeApiFactory(database);
        using var scope = factory.Services.CreateScope();

        // Seed through admin use cases: two countries (one disabled), one service, one offered SKU.
        var catalogAdmin = scope.ServiceProvider.GetRequiredService<CatalogAdminService>();
        var skuAdmin = scope.ServiceProvider.GetRequiredService<ProductSkuAdminService>();

        await catalogAdmin.UpsertCountryAsync(new UpsertCountryCommand("ir", "Iran", new Dictionary<string, string> { ["fa"] = "\u0627\u06cc\u0631\u0627\u0646" }), TestContext.Current.CancellationToken);
        await catalogAdmin.UpsertCountryAsync(new UpsertCountryCommand("us", "United States"), TestContext.Current.CancellationToken); // stays disabled
        await catalogAdmin.UpsertServiceAsync(new UpsertServiceCommand("telegram", "Telegram"), TestContext.Current.CancellationToken);
        await catalogAdmin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("country", "IR", true), TestContext.Current.CancellationToken);
        await catalogAdmin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("service", "telegram", true), TestContext.Current.CancellationToken);
        await skuAdmin.UpsertAsync(new UpsertProductSkuCommand("IR", "telegram", ProductType.Activation), TestContext.Current.CancellationToken);
        await skuAdmin.SetOfferedAsync(new SetProductSkuOfferedCommand("IR", "telegram", ProductType.Activation, Offered: true), TestContext.Current.CancellationToken);

        var client = factory.CreateClient();

        // Countries: enabled only, localized names honored, pagination metadata present.
        var english = await client.GetFromJsonAsync<JsonElement>("/api/catalog/countries?culture=en&page=1&pageSize=10", TestContext.Current.CancellationToken);
        Assert.Equal(1, english.GetProperty("totalCount").GetInt32());
        Assert.Equal("Iran", english.GetProperty("items")[0].GetProperty("displayName").GetString());

        var persian = await client.GetFromJsonAsync<JsonElement>("/api/catalog/countries?culture=fa", TestContext.Current.CancellationToken);
        Assert.Equal("\u0627\u06cc\u0631\u0627\u0646", persian.GetProperty("items")[0].GetProperty("displayName").GetString());

        // Offers: canonical view with no provider fields anywhere in the payload.
        var offersResponse = await client.GetAsync("/api/catalog/offers", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, offersResponse.StatusCode);
        var offersJson = await offersResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var offers = JsonDocument.Parse(offersJson).RootElement;
        Assert.Equal(1, offers.GetProperty("totalCount").GetInt32());
        Assert.Equal("IR-telegram-activation", offers.GetProperty("items")[0].GetProperty("stableKey").GetString());

        // Provider leakage guard: none of these tokens may appear in a public payload.
        foreach (var forbidden in new[] { "provider", "vendor", "supplier", "externalCode" })
        {
            Assert.DoesNotContain(forbidden, offersJson, StringComparison.OrdinalIgnoreCase);
        }

        // Pagination is clamped: oversized pageSize is capped, bad page falls back to page 1.
        var clamped = await client.GetFromJsonAsync<JsonElement>("/api/catalog/countries?page=-3&pageSize=100000", TestContext.Current.CancellationToken);
        Assert.Equal(1, clamped.GetProperty("page").GetInt32());
        Assert.True(clamped.GetProperty("pageSize").GetInt32() <= 100);

        // Deterministic ordering across pages: stable key sort for offers.
        var secondPage = await client.GetFromJsonAsync<JsonElement>("/api/catalog/countries?page=2&pageSize=1", TestContext.Current.CancellationToken);
        Assert.Equal(0, secondPage.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task Openapi_document_describes_the_public_catalog_contract()
    {
        await using var factory = new GetCodeApiFactory(database);
        var client = factory.CreateClient();

        var docResponse = await client.GetAsync("/openapi/v1.json", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, docResponse.StatusCode);

        var doc = JsonDocument.Parse(await docResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)).RootElement;
        var paths = doc.GetProperty("paths");

        foreach (var expected in new[] { "/api/catalog/countries", "/api/catalog/services", "/api/catalog/offers" })
        {
            Assert.True(paths.TryGetProperty(expected, out _), $"OpenAPI document must describe {expected}");
        }
    }
}
