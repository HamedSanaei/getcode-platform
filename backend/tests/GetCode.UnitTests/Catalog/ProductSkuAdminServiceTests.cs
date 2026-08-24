using GetCode.Application.Catalog;
using GetCode.Domain.Catalog;

namespace GetCode.UnitTests;

/// <summary>
/// M03-002: SKU admin/query use cases with fakes — canonical code resolution,
/// idempotent identity, outbox audit payloads.
/// </summary>
public sealed class ProductSkuAdminServiceTests
{
    private readonly FakeCountryRepository _countries = new();
    private readonly FakeServiceRepository _services = new();
    private readonly FakeSkuRepository _skus = new();
    private readonly FakeOutboxCollector _outbox = new();
    private readonly ProductSkuAdminService _admin;
    private readonly ProductCatalogQueryService _queries;

    public ProductSkuAdminServiceTests()
    {
        var unitOfWork = new FakeUnitOfWork();
        _admin = new ProductSkuAdminService(_countries, _services, _skus, _outbox, unitOfWork);
        _queries = new ProductCatalogQueryService(_countries, _services, _skus);

        var iran = GetCode.Domain.Catalog.Country.Upsert("IR", "Iran", DateTimeOffset.UtcNow);
        iran.SetAvailability(true, DateTimeOffset.UtcNow);
        _countries.Seed(iran);
        var telegram = GetCode.Domain.Catalog.Service.Upsert("telegram", "Telegram", DateTimeOffset.UtcNow);
        telegram.SetAvailability(true, DateTimeOffset.UtcNow);
        _services.Seed(telegram);
    }

    [Fact]
    public async Task Upsert_resolves_codes_and_is_idempotent_on_identity_triple()
    {
        var first = await _admin.UpsertAsync(new UpsertProductSkuCommand("ir", "telegram", ProductType.Activation), TestContext.Current.CancellationToken);
        var second = await _admin.UpsertAsync(new UpsertProductSkuCommand("IR", "Telegram", ProductType.Activation), TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Single(_skus.All);
    }

    [Fact]
    public async Task Unknown_canonical_references_are_rejected()
    {
        await Assert.ThrowsAsync<CatalogEntryNotFoundException>(() =>
            _admin.UpsertAsync(new UpsertProductSkuCommand("XX", "telegram", ProductType.Activation), TestContext.Current.CancellationToken));
        await Assert.ThrowsAsync<CatalogEntryNotFoundException>(() =>
            _admin.UpsertAsync(new UpsertProductSkuCommand("IR", "nope", ProductType.Activation), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Offering_changes_are_collected_for_audit()
    {
        await _admin.UpsertAsync(new UpsertProductSkuCommand("IR", "telegram", ProductType.Rental), TestContext.Current.CancellationToken);
        await _admin.SetOfferedAsync(new SetProductSkuOfferedCommand("IR", "telegram", ProductType.Rental, Offered: true, CorrelationId: "corr-sku-1"), TestContext.Current.CancellationToken);

        Assert.Contains(_outbox.Collected, m => m.Type == "catalog.product_sku.availability_changed");
        var message = _outbox.Collected.Single(m => m.Type == "catalog.product_sku.availability_changed");
        Assert.Equal("corr-sku-1", message.CorrelationId);
        Assert.Contains("Rental", message.PayloadJson);
    }

    [Fact]
    public async Task Query_composes_display_names_and_hides_unreferenced_skus()
    {
        // A second country that stays disabled (the default for new entries).
        _countries.Seed(GetCode.Domain.Catalog.Country.Upsert("DE", "Germany", DateTimeOffset.UtcNow));

        await _admin.UpsertAsync(new UpsertProductSkuCommand("IR", "telegram", ProductType.Activation), TestContext.Current.CancellationToken);
        await _admin.SetOfferedAsync(new SetProductSkuOfferedCommand("IR", "telegram", ProductType.Activation, true), TestContext.Current.CancellationToken);
        // SKU over a disabled country is not part of any storefront listing.
        await _admin.UpsertAsync(new UpsertProductSkuCommand("DE", "telegram", ProductType.Activation), TestContext.Current.CancellationToken);
        await _admin.SetOfferedAsync(new SetProductSkuOfferedCommand("DE", "telegram", ProductType.Activation, true), TestContext.Current.CancellationToken);

        var views = await _queries.ListOfferedSkusAsync("en", TestContext.Current.CancellationToken);

        var view = Assert.Single(views);
        Assert.Equal("IR-telegram-activation", view.StableKey);
        Assert.Equal("Iran", view.CountryDisplayName);
        Assert.Equal("Telegram", view.ServiceDisplayName);
    }

    private sealed class FakeCountryRepository : ICountryRepository
    {
        public List<GetCode.Domain.Catalog.Country> All { get; } = [];

        public void Seed(GetCode.Domain.Catalog.Country country) => All.Add(country);

        public Task<GetCode.Domain.Catalog.Country?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(c => c.Code == code.Trim().ToUpperInvariant()));

        public Task<GetCode.Domain.Catalog.Country?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(c => c.Id == id));

        public void Add(GetCode.Domain.Catalog.Country country) => All.Add(country);

        public Task<IReadOnlyList<GetCode.Domain.Catalog.Country>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GetCode.Domain.Catalog.Country>>([.. All.Where(c => includeDisabled || c.IsEnabled)]);
    }

    private sealed class FakeServiceRepository : IServiceRepository
    {
        public List<GetCode.Domain.Catalog.Service> All { get; } = [];

        public void Seed(GetCode.Domain.Catalog.Service service) => All.Add(service);

        public Task<GetCode.Domain.Catalog.Service?> FindBySlugAsync(string slug, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(s => s.Slug == slug.Trim().ToLowerInvariant()));

        public Task<GetCode.Domain.Catalog.Service?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(s => s.Id == id));

        public void Add(GetCode.Domain.Catalog.Service service) => All.Add(service);

        public Task<IReadOnlyList<GetCode.Domain.Catalog.Service>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GetCode.Domain.Catalog.Service>>([.. All.Where(s => includeDisabled || s.IsEnabled)]);
    }

    private sealed class FakeSkuRepository : IProductSkuRepository
    {
        public List<GetCode.Domain.Catalog.ProductSku> All { get; } = [];

        public Task<GetCode.Domain.Catalog.ProductSku?> FindAsync(Guid countryId, Guid serviceId, ProductType productType, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(s => s.CountryId == countryId && s.ServiceId == serviceId && s.ProductType == productType));

        public void Add(GetCode.Domain.Catalog.ProductSku sku) => All.Add(sku);

        public Task<IReadOnlyList<GetCode.Domain.Catalog.ProductSku>> ListOfferedAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<GetCode.Domain.Catalog.ProductSku>>([.. All.Where(s => s.IsOffered)]);
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
