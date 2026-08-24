using System.Text.Json;
using GetCode.Application.Catalog;
using GetCode.Domain.Catalog;
using GetCode.Domain.Providers;

namespace GetCode.Application.Providers;

public interface IProviderRepository
{
    Task<ProviderDefinition?> FindByKeyAsync(string providerKey, CancellationToken cancellationToken);
    void Add(ProviderDefinition provider);
}

/// <summary>Durable storage for canonical mappings, scoped per provider.</summary>
public interface IProviderMappingRepository
{
    Task<ProviderMapping?> FindByExternalCodeAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken);
    /// <summary>Reverse lookup used by routing: provider external code to canonical id.</summary>
    Task<Guid?> ResolveCanonicalIdAsync(Guid providerId, MappingKind kind, string externalCode, CancellationToken cancellationToken);
    void Add(ProviderMapping mapping);
}

public sealed record RegisterProviderCommand(string ProviderKey, string DisplayName, bool SupportsActivation = true, bool SupportsRental = false, string? CorrelationId = null);

public sealed record SetProviderEnabledCommand(string ProviderKey, bool Enabled, string? CorrelationId = null);

public sealed record BindCanonicalMappingCommand(string ProviderKey, MappingKind Kind, string ExternalCode, string CanonicalStableKey, string? CorrelationId = null);

/// <summary>
/// Provider registry and mapping administration. Mappings are validated
/// against the live canonical catalog and every change is mirrored into the
/// transactional outbox for audit.
/// </summary>
public sealed class ProviderAdminService(
    IProviderRepository providers,
    IProviderMappingRepository mappings,
    ICountryRepository countries,
    IServiceRepository services,
    IOutboxCollector outbox,
    ICatalogUnitOfWork unitOfWork)
{
    public async Task<Guid> RegisterAsync(RegisterProviderCommand command, CancellationToken cancellationToken)
    {
        var key = command.ProviderKey.Trim().ToLowerInvariant();
        var provider = await providers.FindByKeyAsync(key, cancellationToken);
        if (provider is null)
        {
            provider = ProviderDefinition.Register(key, command.DisplayName, DateTimeOffset.UtcNow);
            provider.SetCapabilities(command.SupportsActivation, command.SupportsRental, DateTimeOffset.UtcNow);
            providers.Add(provider);

            Collect(provider.ProviderKey, "providers.registered", new
            {
                providerKey = provider.ProviderKey,
                displayName = provider.DisplayName,
                supportsActivation = provider.SupportsActivation,
                supportsRental = provider.SupportsRental,
            }, command.CorrelationId);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return provider.Id;
    }

    public async Task SetEnabledAsync(SetProviderEnabledCommand command, CancellationToken cancellationToken)
    {
        var provider = await RequireProviderAsync(command.ProviderKey, cancellationToken);
        provider.SetEnabled(command.Enabled, DateTimeOffset.UtcNow);

        Collect(provider.ProviderKey, "providers.availability_changed", new
        {
            providerKey = provider.ProviderKey,
            enabled = command.Enabled,
        }, command.CorrelationId);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Binds (or re-binds) a provider external code to a canonical entry.
    /// The canonical target must exist — mapping changes are validated here.
    /// </summary>
    public async Task<Guid> BindMappingAsync(BindCanonicalMappingCommand command, CancellationToken cancellationToken)
    {
        var provider = await RequireProviderAsync(command.ProviderKey, cancellationToken);

        Guid canonicalId;
        switch (command.Kind)
        {
            case MappingKind.Country:
                canonicalId = await countries.FindByCodeAsync(command.CanonicalStableKey, cancellationToken) is { } country
                    ? country.Id
                    : throw new CatalogEntryNotFoundException("country", command.CanonicalStableKey);
                break;
            case MappingKind.Service:
                canonicalId = await services.FindBySlugAsync(command.CanonicalStableKey, cancellationToken) is { } service
                    ? service.Id
                    : throw new CatalogEntryNotFoundException("service", command.CanonicalStableKey);
                break;
            default:
                throw new ArgumentException($"Unknown mapping kind '{command.Kind}'.", nameof(command));
        }

        var existing = await mappings.FindByExternalCodeAsync(provider.Id, command.Kind, command.ExternalCode, cancellationToken);
        ProviderMapping mapping;
        if (existing is null)
        {
            mapping = ProviderMapping.Bind(provider.Id, command.Kind, command.ExternalCode, canonicalId);
            mappings.Add(mapping);
        }
        else
        {
            existing.RebindTo(canonicalId);
            mapping = existing;
        }

        Collect(provider.ProviderKey, existing is null ? "providers.mapping.bound" : "providers.mapping.rebound", new
        {
            providerKey = provider.ProviderKey,
            kind = command.Kind.ToString(),
            externalCode = command.ExternalCode.Trim(),
            canonicalStableKey = command.CanonicalStableKey,
        }, command.CorrelationId);

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return mapping.Id;
    }

    private async Task<ProviderDefinition> RequireProviderAsync(string providerKey, CancellationToken cancellationToken) =>
        await providers.FindByKeyAsync(providerKey.Trim().ToLowerInvariant(), cancellationToken)
        ?? throw new CatalogEntryNotFoundException("provider", providerKey);

    private void Collect(string providerKey, string type, object payload, string? correlationId) =>
        outbox.Collect(type, JsonSerializer.Serialize(payload, CatalogAdminService.PayloadOptions), correlationId);
}
