using GetCode.Application.Catalog;
using GetCode.Application.Providers;
using GetCode.Domain.Providers;

namespace GetCode.UnitTests;

/// <summary>
/// M03-003: provider admin use cases with fakes — idempotent registration,
/// validated binding, audited changes.
/// </summary>
public sealed class ProviderAdminServiceTests
{
    private readonly FakeProviderRepository _providers = new();
    private readonly FakeMappingRepository _mappings = new();
    private readonly FakeCatalogRepositories _catalog = new();
    private readonly FakeOutboxCollector _outbox = new();
    private readonly ProviderAdminService _admin;

    public ProviderAdminServiceTests()
    {
        _admin = new ProviderAdminService(_providers, _mappings, _catalog.Countries, _catalog.Services, _outbox, new FakeUnitOfWork());

        var iran = Domain.Catalog.Country.Upsert("IR", "Iran", DateTimeOffset.UtcNow);
        iran.SetAvailability(true, DateTimeOffset.UtcNow);
        _catalog.Countries.Seed(iran);
    }

    [Fact]
    public async Task Registration_is_idempotent_on_provider_key()
    {
        var first = await _admin.RegisterAsync(new RegisterProviderCommand("Tiger-SMS", "Tiger SMS"), TestContext.Current.CancellationToken);
        var second = await _admin.RegisterAsync(new RegisterProviderCommand("tiger-sms", "Tiger SMS renamed"), TestContext.Current.CancellationToken);

        Assert.Equal(first, second);
        Assert.Single(_providers.All);
        // Original display name is preserved; rename flows are explicit admin ops (not silently overwritten).
        Assert.Equal("Tiger SMS", _providers.All[0].DisplayName);
    }

    [Fact]
    public async Task Mapping_binding_requires_existing_canonical_entry()
    {
        await _admin.RegisterAsync(new RegisterProviderCommand("acme", "Acme"), TestContext.Current.CancellationToken);

        var boundId = await _admin.BindMappingAsync(new BindCanonicalMappingCommand("acme", MappingKind.Country, "16", "IR"), TestContext.Current.CancellationToken);
        Assert.NotEqual(Guid.Empty, boundId);
        Assert.Single(_mappings.All);

        await Assert.ThrowsAsync<CatalogEntryNotFoundException>(() =>
            _admin.BindMappingAsync(new BindCanonicalMappingCommand("acme", MappingKind.Country, "17", "XX"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Rebinding_same_external_code_updates_target_and_audits()
    {
        var germany = Domain.Catalog.Country.Upsert("DE", "Germany", DateTimeOffset.UtcNow);
        _catalog.Countries.Seed(germany);

        await _admin.RegisterAsync(new RegisterProviderCommand("acme", "Acme"), TestContext.Current.CancellationToken);
        await _admin.BindMappingAsync(new BindCanonicalMappingCommand("acme", MappingKind.Country, "16", "IR"), TestContext.Current.CancellationToken);
        await _admin.BindMappingAsync(new BindCanonicalMappingCommand("acme", MappingKind.Country, "16", "DE"), TestContext.Current.CancellationToken);

        Assert.Single(_mappings.All); // same external code re-pointed
        Assert.Equal(germany.Id, _mappings.All[0].CanonicalId);

        // Both writes are audited: initial bind + rebound.
        Assert.Contains(_outbox.Collected, m => m.Type == "providers.mapping.bound");
        Assert.Contains(_outbox.Collected, m => m.Type == "providers.mapping.rebound");
    }

    private sealed class FakeProviderRepository : IProviderRepository
    {
        public List<ProviderDefinition> All { get; } = [];

        public Task<ProviderDefinition?> FindByKeyAsync(string providerKey, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(p => p.ProviderKey == providerKey.Trim().ToLowerInvariant()));

        public void Add(ProviderDefinition provider) => All.Add(provider);
    }

    private sealed class FakeMappingRepository : IProviderMappingRepository
    {
        public List<ProviderMapping> All { get; } = [];

        public Task<ProviderMapping?> FindByExternalCodeAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken) =>
            Task.FromResult(All.FirstOrDefault(m => m.ProviderId == providerId && m.Kind == kind && m.ExternalCode == externalCode.Trim()));

        public Task<Guid?> ResolveCanonicalIdAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken) =>
            Task.FromResult(FindByExternalCodeAsync(providerId, kind, externalCode, cancellationToken).Result?.CanonicalId);

        public void Add(ProviderMapping mapping) => All.Add(mapping);
    }

    private sealed class FakeCatalogRepositories
    {
        public FakeCountryRepo Countries { get; } = new();
        public FakeServiceRepo Services { get; } = new();

        internal sealed class FakeCountryRepo : ICountryRepository
        {
            public List<Domain.Catalog.Country> All { get; } = [];

            public void Seed(Domain.Catalog.Country country) => All.Add(country);

            public Task<Domain.Catalog.Country?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
                Task.FromResult(All.FirstOrDefault(c => c.Code == code.Trim().ToUpperInvariant()));

            public Task<Domain.Catalog.Country?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(All.FirstOrDefault(c => c.Id == id));

            public void Add(Domain.Catalog.Country country) => All.Add(country);

            public Task<IReadOnlyList<Domain.Catalog.Country>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<Domain.Catalog.Country>>([.. All.Where(c => includeDisabled || c.IsEnabled)]);
        }

        internal sealed class FakeServiceRepo : IServiceRepository
        {
            public List<Domain.Catalog.Service> All { get; } = [];

            public Task<Domain.Catalog.Service?> FindBySlugAsync(string slug, CancellationToken cancellationToken) =>
                Task.FromResult(All.FirstOrDefault(s => s.Slug == slug.Trim().ToLowerInvariant()));

            public Task<Domain.Catalog.Service?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
                Task.FromResult(All.FirstOrDefault(s => s.Id == id));

            public void Add(Domain.Catalog.Service service) => All.Add(service);

            public Task<IReadOnlyList<Domain.Catalog.Service>> ListAsync(bool includeDisabled, CancellationToken cancellationToken) =>
                Task.FromResult<IReadOnlyList<Domain.Catalog.Service>>([.. All.Where(s => includeDisabled || s.IsEnabled)]);
        }
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
