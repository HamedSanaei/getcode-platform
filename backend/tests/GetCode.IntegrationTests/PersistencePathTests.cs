using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using GetCode.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace GetCode.IntegrationTests;

/// <summary>
/// M00-005 acceptance: a representative persistence path (outbox insert →
/// query through the real Npgsql provider and applied migrations) plus
/// transaction rollback semantics. Runs against an isolated per-run container.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class PersistencePathTests(DatabaseFixture database) : IClassFixture<GetCodeApiFactory>
{
    private readonly DatabaseFixture _database = database;

    private GetCodeApiFactory CreateFactory() => new(_database);

    [Fact]
    public async Task Outbox_message_roundtrips_through_real_postgres()
    {
        await using var factory = CreateFactory();
        var scope = factory.Services.CreateAsyncScope();
        await using (scope)
        {
            var context = scope.ServiceProvider.GetRequiredService<GetCodeDbContext>();
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Type = "test.ping-sent",
                PayloadJson = """{"probe":"m00-005"}""",
                CorrelationId = "corr-m00-005",
            };
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            // New context = fresh change tracker, proving the row is really in PostgreSQL.
            var options = new DbContextOptionsBuilder<GetCodeDbContext>().UseNpgsql(_database.ConnectionString).Options;
            await using var verificationContext = new GetCodeDbContext(options);
            var stored = await verificationContext.OutboxMessages.SingleAsync(
                m => m.Id == message.Id, TestContext.Current.CancellationToken);

            Assert.Equal("test.ping-sent", stored.Type);
            Assert.Equal("corr-m00-005", stored.CorrelationId);
            Assert.Null(stored.ProcessedAtUtc);

            // Mutable dispatch bookkeeping round-trips too.
            stored.ProcessedAtUtc = DateTimeOffset.UtcNow;
            stored.AttemptCount = 1;
            await verificationContext.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task Rolled_back_transaction_leaves_no_rows_behind()
    {
        await using var factory = CreateFactory();
        var options = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;

        long baselineCount;
        await using (var setup = new GetCodeDbContext(options))
        {
            baselineCount = await setup.OutboxMessages.LongCountAsync(TestContext.Current.CancellationToken);
        }

        await using (var context = new GetCodeDbContext(options))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            context.OutboxMessages.Add(new OutboxMessage
            {
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Type = "test.rollback-probe",
                PayloadJson = "{}",
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.RollbackAsync(TestContext.Current.CancellationToken);
        }

        await using (var verification = new GetCodeDbContext(options))
        {
            var countAfterRollback = await verification.OutboxMessages.LongCountAsync(TestContext.Current.CancellationToken);
            Assert.Equal(baselineCount, countAfterRollback);
        }
    }

    [Fact]
    public async Task Committed_transaction_persists_across_contexts()
    {
        await using var factory = CreateFactory();
        var options = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        var id = Guid.NewGuid();

        await using (var context = new GetCodeDbContext(options))
        {
            await using var transaction = await context.Database.BeginTransactionAsync(TestContext.Current.CancellationToken);
            context.OutboxMessages.Add(new OutboxMessage
            {
                Id = id,
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Type = "test.commit-probe",
                PayloadJson = "{}",
            });
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await transaction.CommitAsync(TestContext.Current.CancellationToken);
        }

        await using (var verification = new GetCodeDbContext(options))
        {
            Assert.True(await verification.OutboxMessages.AnyAsync(m => m.Id == id, TestContext.Current.CancellationToken));
        }
    }

    [Fact]
    public async Task Migrations_report_applied_to_the_test_database()
    {
        var options = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(_database.ConnectionString)
            .Options;
        await using var context = new GetCodeDbContext(options);

        Assert.Empty(await context.Database.GetPendingMigrationsAsync(TestContext.Current.CancellationToken));
        Assert.NotEmpty(await context.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken));
    }
}
