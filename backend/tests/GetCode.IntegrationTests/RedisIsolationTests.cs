using GetCode.IntegrationTests.Infrastructure;
using Xunit;

namespace GetCode.IntegrationTests;

/// <summary>
/// M00-005: proves the ephemeral tier can be started isolated (own container,
/// no developer data) and is reachable. Uses a raw RESP PING so no Redis client
/// package is added before a production consumer exists.
/// </summary>
public sealed class RedisIsolationTests(RedisFixture redis) : IClassFixture<RedisFixture>
{
    [Fact]
    public async Task Isolated_redis_container_responds_to_ping()
    {
        Assert.True(await redis.RespondsToPingAsync(TestContext.Current.CancellationToken));
    }
}
