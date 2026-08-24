namespace GetCode.Application.SiteHosts;

/// <summary>
/// Read view over every configured site. Implemented at the composition root
/// from Site Context configuration; lets application services make decisions
/// about *other* sites without reaching into API-layer options objects.
/// </summary>
public interface ISiteCatalog
{
    IReadOnlyList<SiteDescriptor> Sites { get; }

    /// <summary>The canonical site (first IsCanonical entry, else first).</summary>
    SiteDescriptor Canonical { get; }
}
