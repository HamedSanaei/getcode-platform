using Microsoft.AspNetCore.Mvc.Testing;

namespace GetCode.IntegrationTests;

public sealed class ApiLivenessTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ApiLivenessTests(WebApplicationFactory<Program> factory) => _factory = factory;

    [Fact]
    public async Task Live_endpoint_is_reachable()
    {
        using var client = _factory.CreateClient();
        using var response = await client.GetAsync("/health/live", TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
