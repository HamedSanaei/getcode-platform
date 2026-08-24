using GetCode.Domain.Common;

namespace GetCode.Domain.Providers;

/// <summary>
/// A registered external virtual-number provider. The stable identity is the
/// GetCode-assigned provider key; credentials live only in secret storage and
/// never enter this model.
/// </summary>
public sealed partial class ProviderDefinition : AggregateRoot<Guid>
{
    private ProviderDefinition(Guid id, string providerKey, string displayName)
        : base(id)
    {
        ProviderKey = providerKey;
        DisplayName = displayName;
    }

    /// <summary>EF materialization constructor.</summary>
    private ProviderDefinition()
        : base(Guid.Empty)
    {
        ProviderKey = string.Empty;
        DisplayName = string.Empty;
    }

    public string ProviderKey { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsEnabled { get; private set; }
    public bool SupportsActivation { get; private set; }
    public bool SupportsRental { get; private set; }

    public static ProviderDefinition Register(string? providerKey, string? displayName, DateTimeOffset nowUtc, Guid? id = null)
    {
        var normalizedKey = providerKey?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedKey) || !KeyPattern().IsMatch(normalizedKey))
        {
            throw new ArgumentException("Provider key must be lowercase kebab-case (e.g. 'tiger-sms').", nameof(providerKey));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        var provider = new ProviderDefinition(id ?? Guid.CreateVersion7(), normalizedKey, displayName.Trim());
        provider.Raise(new ProviderRegistered(provider.Id, provider.ProviderKey, nowUtc));
        return provider;
    }

    public void SetEnabled(bool enabled, DateTimeOffset nowUtc)
    {
        if (IsEnabled == enabled)
        {
            return;
        }

        IsEnabled = enabled;
        Raise(new ProviderAvailabilityChanged(ProviderKey, enabled, nowUtc));
    }

    /// <summary>Capability metadata gates which product types the provider may serve.</summary>
    public void SetCapabilities(bool supportsActivation, bool supportsRental, DateTimeOffset nowUtc)
    {
        SupportsActivation = supportsActivation;
        SupportsRental = supportsRental;
        Raise(new ProviderCapabilitiesChanged(ProviderKey, supportsActivation, supportsRental, nowUtc));
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial System.Text.RegularExpressions.Regex KeyPattern();
}

/// <summary>Which canonical catalog concept an external code maps onto.</summary>
public enum MappingKind
{
    Country = 0,
    Service = 1,
}

/// <summary>
/// A provider-owned external code bound to a canonical GetCode identifier.
/// The mapping belongs to the provider capability surface — it never becomes
/// a vendor field on canonical aggregates.
/// </summary>
public sealed class ProviderMapping : Entity<Guid>
{
    private ProviderMapping(Guid id, Guid providerId, MappingKind kind, string externalCode, Guid canonicalId)
        : base(id)
    {
        ProviderId = providerId;
        Kind = kind;
        ExternalCode = externalCode;
        CanonicalId = canonicalId;
    }

    /// <summary>EF materialization constructor.</summary>
    private ProviderMapping()
        : base(Guid.Empty)
    {
        ProviderId = Guid.Empty;
        Kind = default;
        ExternalCode = string.Empty;
        CanonicalId = Guid.Empty;
    }

    public Guid ProviderId { get; private set; }
    public MappingKind Kind { get; private set; }
    public string ExternalCode { get; private set; }
    public Guid CanonicalId { get; private set; }

    /// <summary>
    /// Pure factory: validates and binds. Audit emission is the application
    /// layer's job (transactional outbox), so this entity carries no events.
    /// </summary>
    public static ProviderMapping Bind(Guid providerId, MappingKind kind, string? externalCode, Guid canonicalId, Guid? existingId = null)
    {
        if (providerId == Guid.Empty || canonicalId == Guid.Empty)
        {
            throw new ArgumentException("Both provider and canonical identifiers are required.");
        }

        if (string.IsNullOrWhiteSpace(externalCode))
        {
            throw new ArgumentException("External code is required.", nameof(externalCode));
        }

        var code = externalCode.Trim();
        if (code.Length > MaxExternalCodeLength)
        {
            throw new ArgumentException($"External code exceeds {MaxExternalCodeLength} characters.", nameof(externalCode));
        }

        return new ProviderMapping(existingId ?? Guid.CreateVersion7(), providerId, kind, code, canonicalId);
    }

    /// <summary>Re-points an existing mapping to another canonical target.</summary>
    public void RebindTo(Guid canonicalId)
    {
        if (canonicalId == Guid.Empty)
        {
            throw new ArgumentException("A canonical identifier is required.", nameof(canonicalId));
        }

        CanonicalId = canonicalId;
    }

    public const int MaxExternalCodeLength = 128;
}
