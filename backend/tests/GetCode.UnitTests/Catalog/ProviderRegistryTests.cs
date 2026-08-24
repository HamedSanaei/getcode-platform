using GetCode.Domain.Providers;

namespace GetCode.UnitTests;

public sealed class ProviderRegistryTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Register_normalizes_key_and_starts_disabled()
    {
        var provider = ProviderDefinition.Register("Tiger-SMS", "Tiger SMS", Now);

        Assert.Equal("tiger-sms", provider.ProviderKey);
        Assert.False(provider.IsEnabled, "providers start disabled until configured");
        Assert.Contains(provider.DomainEvents, e => e is ProviderRegistered);
    }

    [Theory]
    [InlineData("")]
    [InlineData("Tiger SMS")]
    [InlineData("-x")]
    [InlineData("x--y")]
    public void Invalid_provider_keys_are_rejected(string key)
    {
        Assert.ThrowsAny<ArgumentException>(() => ProviderDefinition.Register(key, "Whatever", Now));
    }

    [Fact]
    public void Capability_flags_are_explicit_metadata()
    {
        var provider = ProviderDefinition.Register("acme", "Acme", Now);

        provider.SetCapabilities(supportsActivation: true, supportsRental: false, Now);

        Assert.True(provider.SupportsActivation);
        Assert.False(provider.SupportsRental);
    }

    [Fact]
    public void Availability_toggle_is_idempotent()
    {
        var provider = ProviderDefinition.Register("acme", "Acme", Now);

        provider.SetEnabled(true, Now);
        provider.SetEnabled(true, Now);

        Assert.Single(provider.DomainEvents.OfType<ProviderAvailabilityChanged>());
    }
}

/// <summary>
/// M03-003 mapping invariants: external codes bind to canonical ids, belong to
/// the provider capability surface, and are validated on write.
/// </summary>
public sealed class ProviderMappingTests
{
    private static readonly Guid ProviderId = Guid.CreateVersion7();
    private static readonly Guid CountryId = Guid.CreateVersion7();

    [Fact]
    public void Bind_creates_mapping_from_external_code_to_canonical_id()
    {
        var mapping = ProviderMapping.Bind(ProviderId, MappingKind.Country, "  16 ", CountryId);

        Assert.Equal("16", mapping.ExternalCode); // trimmed, case preserved (provider-side codes may be case-sensitive)
        Assert.Equal(MappingKind.Country, mapping.Kind);
        Assert.Equal(CountryId, mapping.CanonicalId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Missing_external_code_is_rejected(string? code)
    {
        Assert.ThrowsAny<ArgumentException>(() => ProviderMapping.Bind(ProviderId, MappingKind.Country, code!, CountryId));
    }

    [Fact]
    public void Empty_provider_or_canonical_ids_are_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() => ProviderMapping.Bind(Guid.Empty, MappingKind.Country, "16", CountryId));
        Assert.ThrowsAny<ArgumentException>(() => ProviderMapping.Bind(ProviderId, MappingKind.Country, "16", Guid.Empty));
    }

    [Fact]
    public void Overlong_external_codes_are_rejected()
    {
        Assert.ThrowsAny<ArgumentException>(() =>
            ProviderMapping.Bind(ProviderId, MappingKind.Service, new string('x', ProviderMapping.MaxExternalCodeLength + 1), CountryId));
    }

    [Fact]
    public void Rebind_repoints_canonical_target_with_validation()
    {
        var mapping = ProviderMapping.Bind(ProviderId, MappingKind.Country, "16", CountryId);
        var newTarget = Guid.CreateVersion7();

        mapping.RebindTo(newTarget);

        Assert.Equal(newTarget, mapping.CanonicalId);
        Assert.ThrowsAny<ArgumentException>(() => mapping.RebindTo(Guid.Empty));
    }
}
