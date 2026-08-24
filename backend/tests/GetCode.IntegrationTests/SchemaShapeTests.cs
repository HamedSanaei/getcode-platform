using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GetCode.IntegrationTests;

/// <summary>
/// M00-006: executable schema snapshot review. Asserts that the applied
/// migration set produces exactly the documented snake_case naming scheme
/// (docs/architecture/DATABASE.md) on a real PostgreSQL instance.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class SchemaShapeTests(DatabaseFixture database)
{
    [Fact]
    public async Task Outbox_schema_follows_the_naming_policy()
    {
        var options = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;
        await using var context = new GetCodeDbContext(options);
        var connection = (Npgsql.NpgsqlConnection)context.Database.GetDbConnection();
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        var columns = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT column_name FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = 'outbox_messages'
                ORDER BY ordinal_position
                """;
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                columns.Add(reader.GetString(0));
            }
        }

        Assert.Equal(
            ["id", "occurred_at_utc", "type", "payload_json", "correlation_id", "processed_at_utc", "attempt_count", "last_error_code"],
            columns);

        var constraintNames = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT conname FROM pg_constraint
                WHERE conrelid = 'public.outbox_messages'::regclass
                """;
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                constraintNames.Add(reader.GetString(0));
            }
        }

        Assert.Contains("pk_outbox_messages", constraintNames);

        var indexNames = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = """
                SELECT indexname FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'outbox_messages'
                """;
            await using var reader = await command.ExecuteReaderAsync(TestContext.Current.CancellationToken);
            while (await reader.ReadAsync(TestContext.Current.CancellationToken))
            {
                indexNames.Add(reader.GetString(0));
            }
        }

        Assert.Contains("ix_outbox_messages__processed_at_utc_occurred_at_utc", indexNames);
    }
}
