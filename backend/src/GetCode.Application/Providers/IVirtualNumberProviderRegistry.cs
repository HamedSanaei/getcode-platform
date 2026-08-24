namespace GetCode.Application.Providers;

/// <summary>
/// M04-007: provider registry — the routing/failover layer resolves adapters
/// by canonical provider key instead of hard-coded branches. Business code
/// never references concrete adapter types.
/// </summary>
public interface IVirtualNumberProviderRegistry
{
    /// <summary>All registered adapters in stable (key-ordinal) order.</summary>
    IReadOnlyList<IVirtualNumberProvider> Providers { get; }

    /// <summary>True when a provider with this key is registered.</summary>
    bool Contains(string providerKey);

    /// <summary>Resolve by canonical key, or null when unknown/unregistered.</summary>
    IVirtualNumberProvider? GetByKey(string providerKey);
}
