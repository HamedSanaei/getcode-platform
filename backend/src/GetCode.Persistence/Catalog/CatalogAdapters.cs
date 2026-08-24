using GetCode.Application.Catalog;
using GetCode.Domain.Catalog;
using GetCode.Domain.Providers;
using GetCode.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Catalog;

internal sealed class CountryRepository(GetCodeDbContext context) : ICountryRepository
{
    public Task<Country?> FindByCodeAsync(string code, CancellationToken cancellationToken) =>
        context.Countries.FirstOrDefaultAsync(c => c.Code == code.Trim().ToUpperInvariant(), cancellationToken);

    public Task<Country?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Countries.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public void Add(Country country) => context.Countries.Add(country);

    public async Task<IReadOnlyList<Country>> ListAsync(bool includeDisabled, CancellationToken cancellationToken)
    {
        var query = context.Countries.AsQueryable();
        if (!includeDisabled)
        {
            query = query.Where(c => c.IsEnabled);
        }

        return await query
            .OrderBy(c => c.DisplayOrder).ThenBy(c => c.Code)
            .ToListAsync(cancellationToken);
    }
}

internal sealed class ServiceRepository(GetCodeDbContext context) : IServiceRepository
{
    public Task<Service?> FindBySlugAsync(string slug, CancellationToken cancellationToken) =>
        context.Services.FirstOrDefaultAsync(s => s.Slug == slug.Trim().ToLowerInvariant(), cancellationToken);

    public Task<Service?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Services.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public void Add(Service service) => context.Services.Add(service);

    public async Task<IReadOnlyList<Service>> ListAsync(bool includeDisabled, CancellationToken cancellationToken)
    {
        var query = context.Services.AsQueryable();
        if (!includeDisabled)
        {
            query = query.Where(s => s.IsEnabled);
        }

        return await query
            .OrderBy(s => s.DisplayOrder).ThenBy(s => s.Slug)
            .ToListAsync(cancellationToken);
    }
}

internal sealed class ProductSkuRepository(GetCodeDbContext context) : IProductSkuRepository
{
    public Task<ProductSku?> FindAsync(Guid countryId, Guid serviceId, ProductType productType, CancellationToken cancellationToken) =>
        context.ProductSkus.FirstOrDefaultAsync(s =>
            s.CountryId == countryId && s.ServiceId == serviceId && s.ProductType == productType, cancellationToken);

    public void Add(ProductSku sku) => context.ProductSkus.Add(sku);

    public async Task<IReadOnlyList<ProductSku>> ListOfferedAsync(CancellationToken cancellationToken) =>
        await context.ProductSkus.Where(s => s.IsOffered).ToListAsync(cancellationToken);
}

internal sealed class ProviderRepository(GetCodeDbContext context) : GetCode.Application.Providers.IProviderRepository
{
    public Task<ProviderDefinition?> FindByKeyAsync(string providerKey, CancellationToken cancellationToken) =>
        context.Providers.FirstOrDefaultAsync(p => p.ProviderKey == providerKey.Trim().ToLowerInvariant(), cancellationToken);

    public async Task<IReadOnlyList<ProviderDefinition>> ListAsync(CancellationToken cancellationToken) =>
        await context.Providers.OrderBy(p => p.ProviderKey).ToListAsync(cancellationToken);

    public void Add(ProviderDefinition provider) => context.Providers.Add(provider);
}

internal sealed class ProviderMappingRepository(GetCodeDbContext context) : GetCode.Application.Providers.IProviderMappingRepository
{
    public Task<ProviderMapping?> FindByExternalCodeAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken) =>
        context.ProviderMappings.FirstOrDefaultAsync(m =>
            m.ProviderId == providerId && m.Kind == kind && m.ExternalCode == externalCode.Trim(), cancellationToken);

    public Task<Guid?> ResolveCanonicalIdAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken) =>
        context.ProviderMappings
            .Where(m => m.ProviderId == providerId && m.Kind == kind && m.ExternalCode == externalCode.Trim())
            .Select(m => (Guid?)m.CanonicalId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProviderMapping>> ListForProviderAsync(Guid providerId, CancellationToken cancellationToken) =>
        await context.ProviderMappings
            .Where(m => m.ProviderId == providerId)
            .OrderBy(m => m.Kind).ThenBy(m => m.ExternalCode)
            .ToListAsync(cancellationToken);

    public void Add(ProviderMapping mapping) => context.ProviderMappings.Add(mapping);
}

/// <summary>
/// Stamps ambient trace/correlation context onto collected notifications and
/// persists them in the same unit of work as the triggering state change.
/// </summary>
internal sealed class OutboxCollector(GetCodeDbContext context) : IOutboxCollector
{
    public void Collect(string type, string payloadJson, string? correlationId = null) =>
        context.OutboxMessages.Add(OutboxMessage.Create(type, payloadJson, correlationId));
}

internal sealed class CatalogUnitOfWork(GetCodeDbContext context) : ICatalogUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
