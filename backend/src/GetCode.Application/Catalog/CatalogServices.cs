using System.Text.Json;
using GetCode.Domain.Catalog;
using GetCode.Domain.Common;

namespace GetCode.Application.Catalog;

public sealed record UpsertCountryCommand(string Code, string DefaultDisplayName, IReadOnlyDictionary<string, string>? LocalizedNames = null, string? CorrelationId = null);

public sealed record UpsertServiceCommand(string Slug, string DefaultDisplayName, IReadOnlyDictionary<string, string>? LocalizedNames = null, string? CorrelationId = null);

public sealed record SetCatalogAvailabilityCommand(string Kind, string StableKey, bool Enabled, string? CorrelationId = null);

public sealed record SetCatalogDisplayOrderCommand(string Kind, string StableKey, int DisplayOrder, string? CorrelationId = null);

/// <summary>Storefront read model: canonical identity plus owned display metadata only.</summary>
public sealed record CatalogEntryView(Guid Id, string StableKey, string DisplayName, int DisplayOrder);

/// <summary>
/// Admin use cases for the canonical catalog. Every mutation raises domain
/// events and mirrors them into the transactional outbox for audit.
/// </summary>
public sealed class CatalogAdminService(
    ICountryRepository countries,
    IServiceRepository services,
    IOutboxCollector outbox,
    ICatalogUnitOfWork unitOfWork)
{
    private static readonly JsonSerializerOptions PayloadOptions = new(JsonSerializerDefaults.Web);

    public async Task<Guid> UpsertCountryAsync(UpsertCountryCommand command, CancellationToken cancellationToken)
    {
        var normalized = command.Code.Trim().ToUpperInvariant();
        var country = await countries.FindByCodeAsync(normalized, cancellationToken);
        if (country is null)
        {
            country = Country.Upsert(normalized, command.DefaultDisplayName, DateTimeOffset.UtcNow);
            ApplyLocalizedNames(country.SetLocalizedNames, command.LocalizedNames);
            countries.Add(country);
        }
        else
        {
            country.Rename(command.DefaultDisplayName, DateTimeOffset.UtcNow);
            ApplyLocalizedNames(country.SetLocalizedNames, command.LocalizedNames);
        }

        CollectEvents("catalog.country", country, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return country.Id;
    }

    public async Task<Guid> UpsertServiceAsync(UpsertServiceCommand command, CancellationToken cancellationToken)
    {
        var slug = command.Slug.Trim().ToLowerInvariant();
        var service = await services.FindBySlugAsync(slug, cancellationToken);
        if (service is null)
        {
            service = Service.Upsert(slug, command.DefaultDisplayName, DateTimeOffset.UtcNow);
            ApplyLocalizedNames(service.SetLocalizedNames, command.LocalizedNames);
            services.Add(service);
        }
        else
        {
            service.Rename(command.DefaultDisplayName, DateTimeOffset.UtcNow);
            ApplyLocalizedNames(service.SetLocalizedNames, command.LocalizedNames);
        }

        CollectEvents("catalog.service", service, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return service.Id;
    }

    public async Task SetAvailabilityAsync(SetCatalogAvailabilityCommand command, CancellationToken cancellationToken)
    {
        if (command.Kind == "country")
        {
            var country = await RequireCountryAsync(command.StableKey, cancellationToken);
            country.SetAvailability(command.Enabled, DateTimeOffset.UtcNow);
            CollectEvents("catalog.country", country, command.CorrelationId);
        }
        else if (command.Kind == "service")
        {
            var service = await RequireServiceAsync(command.StableKey, cancellationToken);
            service.SetAvailability(command.Enabled, DateTimeOffset.UtcNow);
            CollectEvents("catalog.service", service, command.CorrelationId);
        }
        else
        {
            throw new ArgumentException($"Unknown catalog kind '{command.Kind}'.", nameof(command));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SetDisplayOrderAsync(SetCatalogDisplayOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.Kind == "country")
        {
            var country = await RequireCountryAsync(command.StableKey, cancellationToken);
            country.SetDisplayOrder(command.DisplayOrder, DateTimeOffset.UtcNow);
            CollectEvents("catalog.country", country, command.CorrelationId);
        }
        else if (command.Kind == "service")
        {
            var service = await RequireServiceAsync(command.StableKey, cancellationToken);
            service.SetDisplayOrder(command.DisplayOrder, DateTimeOffset.UtcNow);
            CollectEvents("catalog.service", service, command.CorrelationId);
        }
        else
        {
            throw new ArgumentException($"Unknown catalog kind '{command.Kind}'.", nameof(command));
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Country> RequireCountryAsync(string stableKey, CancellationToken cancellationToken) =>
        await countries.FindByCodeAsync(stableKey.Trim().ToUpperInvariant(), cancellationToken)
        ?? throw new CatalogEntryNotFoundException("country", stableKey);

    private async Task<Service> RequireServiceAsync(string stableKey, CancellationToken cancellationToken) =>
        await services.FindBySlugAsync(stableKey.Trim().ToLowerInvariant(), cancellationToken)
        ?? throw new CatalogEntryNotFoundException("service", stableKey);

    /// <summary>Mirrors aggregate events into the outbox with a stable payload contract.</summary>
    private void CollectEvents(string prefix, Country country, string? correlationId)
    {
        foreach (var domainEvent in country.DomainEvents)
        {
            outbox.Collect($"{prefix}.{EventType(domainEvent)}", JsonSerializer.Serialize(new
            {
                countryId = country.Id,
                code = country.Code,
                eventType = domainEvent.GetType().Name,
            }, PayloadOptions), correlationId);
        }

        country.ClearDomainEvents();
    }

    private void CollectEvents(string prefix, Service service, string? correlationId)
    {
        foreach (var domainEvent in service.DomainEvents)
        {
            outbox.Collect($"{prefix}.{EventType(domainEvent)}", JsonSerializer.Serialize(new
            {
                serviceId = service.Id,
                slug = service.Slug,
                eventType = domainEvent.GetType().Name,
            }, PayloadOptions), correlationId);
        }

        service.ClearDomainEvents();
    }

    private static void ApplyLocalizedNames(Action<IEnumerable<LocalizedCatalogName>> setter, IReadOnlyDictionary<string, string>? localizedNames)
    {
        if (localizedNames is null || localizedNames.Count == 0)
        {
            return;
        }

        setter(localizedNames.Select(kv => LocalizedCatalogName.Create(kv.Key, kv.Value)));
    }

    private static string EventType(IDomainEvent domainEvent) => domainEvent switch
    {
        CountryUpserted => "upserted",
        ServiceUpserted => "upserted",
        CatalogAvailabilityChanged => "availability_changed",
        CatalogOrderChanged => "order_changed",
        _ => "changed",
    };
}

public sealed class CatalogEntryNotFoundException(string kind, string stableKey)
    : Exception($"Catalog {kind} '{stableKey}' was not found.")
{
    public string Kind { get; } = kind;
    public string StableKey { get; } = stableKey;
}

/// <summary>Read path used by the storefront and admin listings.</summary>
public sealed class CatalogQueryService(ICountryRepository countries, IServiceRepository services)
{
    public async Task<IReadOnlyList<CatalogEntryView>> ListCountriesAsync(bool includeDisabled, string cultureCode, CancellationToken cancellationToken)
    {
        var list = await countries.ListAsync(includeDisabled, cancellationToken);
        return [.. list.Select(c => new CatalogEntryView(c.Id, c.Code, c.DisplayNameFor(cultureCode), c.DisplayOrder))];
    }

    public async Task<IReadOnlyList<CatalogEntryView>> ListServicesAsync(bool includeDisabled, string cultureCode, CancellationToken cancellationToken)
    {
        var list = await services.ListAsync(includeDisabled, cancellationToken);
        return [.. list.Select(s => new CatalogEntryView(s.Id, s.Slug, s.DisplayNameFor(cultureCode), s.DisplayOrder))];
    }
}
