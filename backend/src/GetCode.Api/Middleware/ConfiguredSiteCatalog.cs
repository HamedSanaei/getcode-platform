using GetCode.Application.SiteHosts;
using Microsoft.Extensions.Options;

namespace GetCode.Api.Middleware;

/// <summary>Exposes every configured site to application services (M02-003).</summary>
internal sealed class ConfiguredSiteCatalog : ISiteCatalog
{
    public ConfiguredSiteCatalog(IOptions<SiteHostOptions> options)
    {
        var hosts = options.Value.Hosts
            .Select(h => new SiteDescriptor(h.Key, h.Host, new Uri(h.PublicBaseUrl), h.BrandKey, h.IsCanonical))
            .ToList();
        Sites = hosts;
        Canonical = hosts.FirstOrDefault(x => x.IsCanonical) ?? hosts.First();
    }

    public IReadOnlyList<SiteDescriptor> Sites { get; }
    public SiteDescriptor Canonical { get; }
}
