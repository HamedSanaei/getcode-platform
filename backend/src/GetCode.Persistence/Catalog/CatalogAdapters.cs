using GetCode.Application.Catalog;
using GetCode.Domain.Catalog;
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
