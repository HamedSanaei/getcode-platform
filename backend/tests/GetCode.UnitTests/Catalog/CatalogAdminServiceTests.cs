using System.Text.Json;
using GetCode.Application.Catalog;

namespace GetCode.UnitTests;

/// <summary>
/// M03-001: admin use cases with fakes — verifies idempotent upserts, audit
/// outbox payloads, and that read models expose only owned display metadata.
/// </summary>
public sealed class CatalogAdminServiceTests
{
    private readonly FakeCountryRepository _countries = new();
    private readonly FakeServiceRepository _services = new();
    private readonly FakeOutboxCollector _outbox = new();
    private readonly CatalogAdminService _admin;
    private readonly CatalogQueryService _queries;

    public CatalogAdminServiceTests()
    {
        var unitOfWork = new FakeUnitOfWork();
        _admin = new CatalogAdminService(_countries, _services, _outbox, unitOfWork);
        _queries = new CatalogQueryService(_countries, _services);
    }

    [Fact]
    public async Task Upsert_country_is_idempotent_on_code()
    {
        var first = await _admin.UpsertCountryAsync(new UpsertCountryCommand("ir", "Iran"), TestContext.Current.CancellationToken);
        var second = await _admin.UpsertCountryAsync(new UpsertCountryCommand("IR", "Islamic Republic of Iran"), TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Single(_countries.All);

        // Rename is reflected in the stored aggregate.
        Assert.Equal("Islamic Republic of Iran", _countries.All[0].DefaultDisplayName);
    }

    [Fact]
    public async Task Upsert_collects_outbox_audit_with_stable_payload()
    {
        await _admin.UpsertCountryAsync(new UpsertCountryCommand("de", "Germany", new Dictionary<string, string> { ["fa"] = "\u0622\u0644\u0645\u0627\u0646" }), TestContext.Current.CancellationToken);

        var message = Assert.Single(_outbox.Collected);
        Assert.Equal("catalog.country.upserted", message.Type);

        using var payload = JsonDocument.Parse(message.PayloadJson);
        Assert.Equal("DE", payload.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task Availability_change_is_collected_for_audit()
    {
        var id = await _admin.UpsertCountryAsync(new UpsertCountryCommand("de", "Germany"), TestContext.Current.CancellationToken);
        await _admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("country", "DE", Enabled: true), TestContext.Current.CancellationToken);

        Assert.Contains(_outbox.Collected, m => m.Type == "catalog.country.availability_changed");
        Assert.True(_countries.All[0].IsEnabled);
        Assert.Equal(id, _countries.All[0].Id);
    }

    [Fact]
    public async Task Unknown_entry_is_reported_as_not_found()
    {
        await Assert.ThrowsAsync<CatalogEntryNotFoundException>(() =>
            _admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("service", "nope", Enabled: true), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Query_lists_only_enabled_entries_in_display_order()
    {
        await _admin.UpsertCountryAsync(new UpsertCountryCommand("US", "United States"), TestContext.Current.CancellationToken);
        var de = await _admin.UpsertCountryAsync(new UpsertCountryCommand("DE", "Germany"), TestContext.Current.CancellationToken);
        await _admin.SetDisplayOrderAsync(new SetCatalogDisplayOrderCommand("country", "DE", 1), TestContext.Current.CancellationToken);
        await _admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("country", "DE", true), TestContext.Current.CancellationToken);
        await _admin.SetDisplayOrderAsync(new SetCatalogDisplayOrderCommand("country", "US", 2), TestContext.Current.CancellationToken);
        await _admin.SetAvailabilityAsync(new SetCatalogAvailabilityCommand("country", "US", false), TestContext.Current.CancellationToken);

        var visible = await _queries.ListCountriesAsync(includeDisabled: false, cultureCode: "en", TestContext.Current.CancellationToken);
        var all = await _queries.ListCountriesAsync(includeDisabled: true, cultureCode: "en", TestContext.Current.CancellationToken);

        Assert.Single(visible, v => v.StableKey == "DE");
        Assert.Equal(2, all.Count);
        Assert.Collection(visible,
            entry => { Assert.Equal(de, entry.Id); Assert.Equal("Germany", entry.DisplayName); });
    }

    private sealed class FakeCountryRepository : ICountryRepository
    {
        public List<GetCode.Domain.Catalog.Country> All { get; } = [];

        public Task<GetCode.Domain.Catalog.Country?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(c => c.Code == code.Trim().ToUpperInvariant()));

        public Task<GetCode.Domain.Catalog.Country?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(c => c.Id == id));

        public void Add(GetCode.Domain.Catalog.Country country) => All.Add(country);

        public Task<IReadOnlyList<GetCode.Domain.Catalog.Country>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GetCode.Domain.Catalog.Country>>(
                [.. All.Where(c => includeDisabled || c.IsEnabled).OrderBy(c => c.DisplayOrder).ThenBy(c => c.Code)]);
    }

    private sealed class FakeServiceRepository : IServiceRepository
    {
        public List<GetCode.Domain.Catalog.Service> All { get; } = [];

        public Task<GetCode.Domain.Catalog.Service?> FindBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(s => s.Slug == slug.Trim().ToLowerInvariant()));

        public Task<GetCode.Domain.Catalog.Service?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(s => s.Id == id));

        public void Add(GetCode.Domain.Catalog.Service service) => All.Add(service);

        public Task<IReadOnlyList<GetCode.Domain.Catalog.Service>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GetCode.Domain.Catalog.Service>>(
                [.. All.Where(s => includeDisabled || s.IsEnabled).OrderBy(s => s.DisplayOrder).ThenBy(s => s.Slug)]);
    }

    private sealed class FakeOutboxCollector : IOutboxCollector
    {
        public List<(string Type, string PayloadJson, string? CorrelationId)> Collected { get; } = [];

        public void Collect(string type, string payloadJson, string? correlationId = null) =>
            Collected.Add((type, payloadJson, correlationId));
    }

    private sealed class FakeUnitOfWork : ICatalogUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => Task.FromResult(1);
    }
}
