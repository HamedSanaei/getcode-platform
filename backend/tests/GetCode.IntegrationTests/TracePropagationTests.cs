using System.Diagnostics;
using GetCode.Application.Common;
using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using GetCode.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace GetCode.IntegrationTests;

/// <summary>
/// M00-008 verification: ambient W3C trace/correlation context captured at
/// publish time survives into durable outbox rows, so later worker-side
/// processing can join the originating workflow.
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class TracePropagationTests(DatabaseFixture database)
{
    [Fact]
    public async Task Outbox_rows_persist_the_captured_trace_context()
    {
        var options = new DbContextOptionsBuilder<GetCodeDbContext>()
            .UseNpgsql(database.ConnectionString)
            .Options;

        OutboxMessage message;
        using (var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == GetCodeObservability.CoreActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        })
        {
            ActivitySource.AddActivityListener(listener);
            using var activity = GetCodeObservability.CoreActivitySource.StartActivity("outbox.probe");
            Assert.NotNull(activity);
            message = OutboxMessage.Create(
                "test.trace-probe",
                """{"probe":"m00-008"}""",
                correlationId: "corr-trace-test");
            Assert.False(string.IsNullOrEmpty(message.TraceId));
            Assert.False(string.IsNullOrEmpty(message.SpanId));
        }

        await using (var context = new GetCodeDbContext(options))
        {
            context.OutboxMessages.Add(message);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        await using (var verification = new GetCodeDbContext(options))
        {
            var stored = await verification.OutboxMessages.SingleAsync(
                m => m.Id == message.Id, TestContext.Current.CancellationToken);
            Assert.Equal(message.TraceId, stored.TraceId);
            Assert.Equal(message.SpanId, stored.SpanId);
        }
    }

    [Fact]
    public void Outbox_created_without_activity_has_no_trace_context()
    {
        var message = OutboxMessage.Create("test.no-activity", "{}");

        // Activity.Current may legitimately be absent in worker contexts.
        if (Activity.Current is null)
        {
            Assert.Null(message.TraceId);
        }
    }
}
