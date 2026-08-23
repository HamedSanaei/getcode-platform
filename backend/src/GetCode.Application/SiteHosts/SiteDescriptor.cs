namespace GetCode.Application.SiteHosts;

public sealed record SiteDescriptor(
    string Key,
    string Host,
    Uri PublicBaseUri,
    string BrandKey,
    bool IsCanonical);

public interface ICurrentSite
{
    SiteDescriptor Site { get; }
}
