using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using GetCode.IntegrationTests.Infrastructure;

namespace GetCode.IntegrationTests;

/// <summary>
/// M05-002 API verification: quote issuance returns the customer view only;
/// checkout revalidation distinguishes valid / tampered / unknown; provider
/// cost data never crosses the HTTP boundary.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class QuoteApiTests(DatabaseFixture database)
{
    private const string PrimaryHost = "getcode.example";

    [Fact]
    public async Task Quote_lifecycle_issue_validate_tamper_and_unknown()
    {
        await using var factory = new GetCodeApiFactory(database);
        factory.ClientOptions.BaseAddress = new Uri($"https://{PrimaryHost}/"); // __Host-/Secure cookies require https
        var client = factory.CreateClient();

        // CSRF handshake (same pattern as auth tests) for the state-changing POST.
        using var csrfRequest = new HttpRequestMessage(HttpMethod.Get, "/api/auth/csrf");
        csrfRequest.Headers.Host = PrimaryHost;
        var csrfResponse = await client.SendAsync(csrfRequest, TestContext.Current.CancellationToken);
        var csrfBody = await csrfResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(csrfResponse.IsSuccessStatusCode, $"csrf {csrfResponse.StatusCode}: {csrfBody}");
        var csrfPayload = await csrfResponse.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);

        using var issueRequest = new HttpRequestMessage(HttpMethod.Post, "/api/quotes")
        {
            Content = JsonContent.Create(new { CountryKey = "RU", ServiceKey = "telegram", ProductTypeKey = "activation", ProviderKey = "fake", ProviderCostAmount = 100m, CostCurrency = "RUB" }),
        };
        issueRequest.Headers.Host = PrimaryHost;
        issueRequest.Headers.Add("Cookie", csrfResponse.Headers.GetValues("Set-Cookie").Single().Split(';')[0]);
        issueRequest.Headers.Add("X-XSRF-TOKEN", csrfPayload.GetProperty("requestToken").GetString()!);
        issueRequest.Headers.Add("Origin", $"https://{PrimaryHost}");
        var issueResponse = await client.SendAsync(issueRequest, TestContext.Current.CancellationToken);
        var issueBody = await issueResponse.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.True(issueResponse.StatusCode == HttpStatusCode.Created, $"status {issueResponse.StatusCode}: {issueBody}");
        var quote = await issueResponse.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: TestContext.Current.CancellationToken);
        var quoteId = quote.GetProperty("quoteId").GetGuid();
        var amount = quote.GetProperty("amount").GetDecimal();

        // Customer view never carries provider-cost data.
        var raw = quote.GetRawText();
        Assert.DoesNotContain("cost", raw, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("providerKey", raw, StringComparison.OrdinalIgnoreCase);

        // Valid checkout revalidation.
        var ok = await client.GetAsync($"/api/quotes/{quoteId}?expectedAmount={amount}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);

        // Tampered amount conflicts with the stored authoritative snapshot.
        var tampered = await client.GetAsync($"/api/quotes/{quoteId}?expectedAmount={amount + 0.01m}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Conflict, tampered.StatusCode);

        // Unknown quote id.
        var unknown = await client.GetAsync($"/api/quotes/{Guid.NewGuid()}?expectedAmount={amount}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, unknown.StatusCode);
    }
}
