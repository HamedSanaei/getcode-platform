using GetCode.Application.SiteHosts;

namespace GetCode.Api.Middleware;

public sealed class CurrentSiteAccessor : ICurrentSite
{
    private SiteDescriptor? _site;

    public SiteDescriptor Site => _site ?? throw new InvalidOperationException("Site context has not been resolved for this request.");

    internal void Set(SiteDescriptor site) => _site = site;
}
