using GetCode.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace GetCode.IntegrationTests.Infrastructure;

/// <summary>
/// M00-005: isolated per-run PostgreSQL container. Never touches developer
/// databases or the compose volumes; each run gets its own container, database
/// and applied migration set. Shared across all test classes in the
/// "database" collection so the suite pays container startup once.
/// </summary>
public sealed class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine")
        .WithDatabase("getcode_tests")
        .WithUsername("getcode")
        .WithPassword("getcode-local-tests")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        var optionsBuilder = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(ConnectionString);
        await using var context = new GetCodeDbContext(optionsBuilder.Options);
        await context.Database.MigrateAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}

[CollectionDefinition(DatabaseCollection.CollectionName)]
public sealed class DatabaseCollection : ICollectionFixture<DatabaseFixture>
{
    public const string CollectionName = "database";
}
