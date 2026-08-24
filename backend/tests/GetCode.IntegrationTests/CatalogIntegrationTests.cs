using GetCode.Application.Catalog;
using GetCode.Application.Common;
using GetCode.IntegrationTests.Infrastructure;
using GetCode.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace GetCode.IntegrationTests;

/// <summary>
/// M03-001 verification: canonical catalog against real PostgreSQL (unique
/// stable keys, owned localization rows, durable outbox audit of admin changes).
/// </summary>
[Collection(DatabaseCollection.CollectionName)]
public sealed class CatalogIntegrationTests(DatabaseFixture database)
{
    private sealed class CatalogScope : IAsyncDisposable
    {
        public CatalogScope(DatabaseFixture databaseFixture)
        {
            Factory = new GetCodeApiFactory(databaseFixture);
            ServiceScope = Factory.Services.CreateScope();
        }

        private GetCodeApiFactory Factory { get; }
        private IServiceScope ServiceScope { get; }

        public CatalogAdminService Admin => ServiceScope.ServiceProvider.GetRequiredService<CatalogAdminService>();

        public CatalogQueryService Queries => ServiceScope.ServiceProvider.GetRequiredService<CatalogQueryService>();

        public GetCodeDbContext NewContext() => ServiceScope.ServiceProvider.GetRequiredService<GetCodeDbContext>();

        /// <summary>
        /// The database fixture lives for the whole collection, so catalog rows
        /// persist between tests. Each test starts from a clean catalog.
        /// </summary>
        public async ValueTask ResetCatalogAsync()
        {
            var context = NewContext();
            await context.Countries.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await context.Services.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
            await context.OutboxMessages.Where(m => m.Type!.StartsWith("catalog.")).ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            ServiceScope.Dispose();
            await Factory.DisposeAsync();
            GC.SuppressFinalize(this);
        }
    }

    [Fact]
    public async Task Upserted_country_is_queryable_with_localization_and_unique_code()
    {
        await using var catalog = new CatalogScope(database);
        await catalog.ResetCatalogAsync();

        await catalog.Admin.UpsertCountryAsync(new UpsertCountryCommand(
            "ir", "Iran", new Dictionary<string, string> { ["fa"] = "\u0627\u06cc\u0631\u0627\u0646" }), TestContext.Current.CancellationToken);

        // Same code upsert must not create a second row.
        await catalog.Admin.UpsertCountryAsync(new UpsertCountryCommand("IR", "Iran"), TestContext.Current.CancellationToken);

        var context = catalog.NewContext();
        Assert.Equal(1, await context.Countries.CountAsync(TestContext.Current.CancellationToken));

        var country = await context.Countries.Include(c => c.LocalizedNames).SingleAsync(TestContext.Current.CancellationToken);
        Assert.Equal("IR", country.Code);
        Assert.Equal("\u0627\u06cc\u0631\u0627\u0646", country.DisplayNameFor("fa"));
    }

    [Fact]
    public async Task Admin_changes_land_in_outbox_with_trace_context()
    {
        await using var catalog = new CatalogScope(database);
        await catalog.ResetCatalogAsync();

        // StartActivity only returns an activity with a listener attached.
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == GetCodeObservability.CoreActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        using var activity = GetCodeObservability.CoreActivitySource.StartActivity("catalog-test");
        await catalog.Admin.UpsertCountryAsync(new UpsertCountryCommand("de", "Germany"), TestContext.Current.CancellationToken);
        await catalog.Admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("country", "DE", true, CorrelationId: "corr-catalog-1"), TestContext.Current.CancellationToken);
        activity?.Dispose();

        var context = catalog.NewContext();
        var messages = await context.OutboxMessages
            .Where(m => m.Type!.StartsWith("catalog."))
            .OrderBy(m => m.OccurredAtUtc)
            .ToListAsync(TestContext.Current.CancellationToken);

        Assert.Contains(messages, m => m.Type == "catalog.country.upserted");
        Assert.Contains(messages, m => m.Type == "catalog.country.availability_changed");

        var availability = messages.Single(m => m.Type == "catalog.country.availability_changed");
        Assert.Equal("corr-catalog-1", availability.CorrelationId);
        Assert.False(string.IsNullOrEmpty(availability.TraceId), "outbox rows carry W3C trace context");
    }

    [Fact]
    public async Task Disabled_entries_are_hidden_from_storefront_queries()
    {
        await using var catalog = new CatalogScope(database);
        await catalog.ResetCatalogAsync();

        await catalog.Admin.UpsertServiceAsync(new UpsertServiceCommand("telegram", "Telegram"), TestContext.Current.CancellationToken);
        await catalog.Admin.UpsertServiceAsync(new UpsertServiceCommand("whatsapp", "WhatsApp"), TestContext.Current.CancellationToken);
        await catalog.Admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("service", "telegram", true), TestContext.Current.CancellationToken);
        // whatsapp stays in its default disabled state; telegram is enabled.
        var visible = await catalog.Queries.ListServicesAsync(includeDisabled: false, cultureCode: "en", TestContext.Current.CancellationToken);

        Assert.Single(visible, v => v.StableKey == "telegram");
    }
}
