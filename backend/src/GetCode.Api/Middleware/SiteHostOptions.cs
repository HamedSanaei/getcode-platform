namespace GetCode.Api.Middleware;

public sealed class SiteHostOptions
{
    public const string SectionName = "SiteHosts";
    public bool RejectUnknownHosts { get; init; } = true;
    public string CanonicalKey { get; init; } = "primary";
    public List<SiteHostEntry> Hosts { get; init; } = [];
}

public sealed class SiteHostEntry
{
    public required string Key { get; init; }
    public required string Host { get; init; }
    public required string PublicBaseUrl { get; init; }
    public required string BrandKey { get; init; }
    public bool IsCanonical { get; init; }
}
