using GetCode.Application.Providers;

namespace GetCode.Infrastructure.Providers;

/// <summary>M04-007: registry over every registered adapter (key-ordinal order, duplicate keys rejected).</summary>
public sealed class VirtualNumberProviderRegistry(IEnumerable<IVirtualNumberProvider> providers) : IVirtualNumberProviderRegistry
{
    private readonly Dictionary<string, IVirtualNumberProvider> _byKey =
        providers.GroupBy(p => p.ProviderKey, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    public IReadOnlyList<IVirtualNumberProvider> Providers => [.. _byKey.Values.OrderBy(p => p.ProviderKey, StringComparer.Ordinal)];

    public bool Contains(string providerKey) => _byKey.ContainsKey(providerKey);

    public IVirtualNumberProvider? GetByKey(string providerKey) =>
        _byKey.TryGetValue(providerKey, out var provider) ? provider : null;
}
