using GetCode.Domain.Catalog;

namespace GetCode.Application.Catalog;

public interface ICountryRepository
{
    Task<Country?> FindByCodeAsync(string code, CancellationToken cancellationToken);
    Task<Country?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Country country);
    /// <summary>Enabled-then-display-order listing for storefront queries.</summary>
    Task<IReadOnlyList<Country>> ListAsync(bool includeDisabled, CancellationToken cancellationToken);
}

public interface IServiceRepository
{
    Task<Service?> FindBySlugAsync(string slug, CancellationToken cancellationToken);
    Task<Service?> FindByIdAsync(Guid id, CancellationToken cancellationToken);
    void Add(Service service);
    Task<IReadOnlyList<Service>> ListAsync(bool includeDisabled, CancellationToken cancellationToken);
}

/// <summary>
/// Collects durable notifications for the transactional outbox. The persistence
/// adapter stamps trace context and persists them in the same unit of work as
/// the state change, so admin catalog changes are always auditable.
/// </summary>
public interface IOutboxCollector
{
    void Collect(string type, string payloadJson, string? correlationId = null);
}

public interface ICatalogUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
