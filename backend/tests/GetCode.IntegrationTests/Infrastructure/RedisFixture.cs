using System.Net.Sockets;
using System.Text;
using Testcontainers.Redis;
using Xunit;

namespace GetCode.IntegrationTests.Infrastructure;

/// <summary>
/// M00-005: opt-in isolated Redis container for suites that need the
/// ephemeral tier. Started only when a test class declares it, so database-only
/// suites do not pay startup cost. Connectivity is proven with a raw RESP PING
/// to avoid pulling a Redis client package before a consumer exists.
/// </summary>
public sealed class RedisFixture : IAsyncLifetime
{
    private readonly RedisContainer _container = new RedisBuilder("redis:8-alpine")
        .Build();

    public string Endpoint => _container.GetConnectionString();

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    /// <summary>Raw RESP handshake; returns true on +PONG.</summary>
    public async Task<bool> RespondsToPingAsync(CancellationToken cancellationToken)
    {
        var endpointParts = Endpoint.Replace("redis://", string.Empty).Split(':');
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        await socket.ConnectAsync(endpointParts[0], int.Parse(endpointParts[1]), cancellationToken);
        await using var stream = new NetworkStream(socket, ownsSocket: true);

        var pingBytes = Encoding.ASCII.GetBytes("*1\r\n$4\r\nPING\r\n");
        await stream.WriteAsync(pingBytes, cancellationToken);

        var buffer = new byte[7];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        return read >= 5 && buffer[0] == (byte)'+' && Encoding.ASCII.GetString(buffer, 0, read).Contains("PONG", StringComparison.Ordinal);
    }
}
