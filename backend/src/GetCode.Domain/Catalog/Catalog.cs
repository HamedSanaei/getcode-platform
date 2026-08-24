using GetCode.Domain.Common;

namespace GetCode.Domain.Catalog;

/// <summary>
/// A localized display name for a catalog entry. Display metadata is owned by
/// GetCode - it is never derived from a provider's labels.
/// </summary>
public sealed partial record LocalizedCatalogName
{
    public string CultureCode { get; }
    public string DisplayName { get; }

    private LocalizedCatalogName(string cultureCode, string displayName)
    {
        CultureCode = cultureCode;
        DisplayName = displayName;
    }

    public static LocalizedCatalogName Create(string? cultureCode, string? displayName)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            throw new ArgumentException("Culture code is required.", nameof(cultureCode));
        }

        // Normalize 'EN', 'fa', 'fa-ir' → canonical 'en' / 'fa-IR'.
        var parts = cultureCode.Trim().Split('-', StringSplitOptions.TrimEntries);
        var normalized = parts.Length == 2
            ? $"{parts[0].ToLowerInvariant()}-{parts[1].ToUpperInvariant()}"
            : parts[0].ToLowerInvariant();
        if (!CulturePattern().IsMatch(normalized))
        {
            throw new ArgumentException("Culture code must look like 'en' or 'fa-IR'.", nameof(cultureCode));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        var name = displayName.Trim();
        if (name.Length > MaxDisplayNameLength)
        {
            throw new ArgumentException($"Display name exceeds {MaxDisplayNameLength} characters.", nameof(displayName));
        }

        return new LocalizedCatalogName(normalized, name);
    }

    public const int MaxDisplayNameLength = 200;

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z]{2}(-[A-Za-z]{2,4})?$")]
    private static partial System.Text.RegularExpressions.Regex CulturePattern();
}

/// <summary>
/// Canonical country. The stable key is the ISO 3166-1 alpha-2 code, owned by
/// GetCode; provider identifiers are mappings stored elsewhere (M03-003).
/// </summary>
public sealed partial class Country : AggregateRoot<Guid>
{
    private readonly List<LocalizedCatalogName> _localizedNames = [];

    private Country(Guid id, string code, string defaultDisplayName)
        : base(id)
    {
        Code = code;
        DefaultDisplayName = defaultDisplayName;
    }

    /// <summary>EF materialization constructor.</summary>
    private Country()
        : base(Guid.Empty)
    {
        Code = string.Empty;
        DefaultDisplayName = string.Empty;
    }

    public string Code { get; private set; }
    public string DefaultDisplayName { get; private set; }
    public bool IsEnabled { get; private set; }
    public int DisplayOrder { get; private set; }

    public IReadOnlyCollection<LocalizedCatalogName> LocalizedNames => _localizedNames.AsReadOnly();

    /// <summary>Creates or updates a country definition (idempotent admin upsert).</summary>
    public static Country Upsert(string? code, string? defaultDisplayName, DateTimeOffset nowUtc, Guid? id = null)
    {
        var normalizedCode = code?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(normalizedCode) || !CodePattern().IsMatch(normalizedCode))
        {
            throw new ArgumentException("Country code must be an ISO 3166-1 alpha-2 code (e.g. 'IR').", nameof(code));
        }

        var name = RequireDisplayName(defaultDisplayName);
        var country = new Country(id ?? Guid.CreateVersion7(), normalizedCode, name);
        country.Raise(new CountryUpserted(country.Id, country.Code, nowUtc));
        return country;
    }

    public void Rename(string defaultDisplayName, DateTimeOffset nowUtc)
    {
        DefaultDisplayName = RequireDisplayName(defaultDisplayName);
        Raise(new CountryUpserted(Id, Code, nowUtc));
    }

    public void SetLocalizedNames(IEnumerable<LocalizedCatalogName> names)
    {
        _localizedNames.Clear();
        _localizedNames.AddRange(names.GroupBy(n => n.CultureCode, StringComparer.OrdinalIgnoreCase).Select(g => g.First()));
    }

    public void SetAvailability(bool enabled, DateTimeOffset nowUtc)
    {
        if (IsEnabled == enabled)
        {
            return; // idempotent
        }

        IsEnabled = enabled;
        Raise(new CatalogAvailabilityChanged("country", Code, enabled, nowUtc));
    }

    public void SetDisplayOrder(int order, DateTimeOffset nowUtc)
    {
        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Display order must be non-negative.");
        }

        if (DisplayOrder == order)
        {
            return;
        }

        DisplayOrder = order;
        Raise(new CatalogOrderChanged("country", Code, order, nowUtc));
    }

    public string DisplayNameFor(string cultureCode) =>
        _localizedNames.FirstOrDefault(n => string.Equals(n.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? DefaultDisplayName;

    [System.Text.RegularExpressions.GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial System.Text.RegularExpressions.Regex CodePattern();

    private static string RequireDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Default display name is required.", nameof(displayName));
        }

        var name = displayName.Trim();
        if (name.Length > LocalizedCatalogName.MaxDisplayNameLength)
        {
            throw new ArgumentException($"Display name exceeds {LocalizedCatalogName.MaxDisplayNameLength} characters.", nameof(displayName));
        }

        return name;
    }
}

/// <summary>
/// Canonical service (e.g. a messaging platform). Stable key is the GetCode slug.
/// A Service is independent of any country; SKUs bind them (M03-002).
/// </summary>
public sealed partial class Service : AggregateRoot<Guid>
{
    private readonly List<LocalizedCatalogName> _localizedNames = [];

    private Service(Guid id, string slug, string defaultDisplayName)
        : base(id)
    {
        Slug = slug;
        DefaultDisplayName = defaultDisplayName;
    }

    /// <summary>EF materialization constructor.</summary>
    private Service()
        : base(Guid.Empty)
    {
        Slug = string.Empty;
        DefaultDisplayName = string.Empty;
    }

    public string Slug { get; private set; }
    public string DefaultDisplayName { get; private set; }
    public bool IsEnabled { get; private set; }
    public int DisplayOrder { get; private set; }

    public IReadOnlyCollection<LocalizedCatalogName> LocalizedNames => _localizedNames.AsReadOnly();

    public static Service Upsert(string? slug, string? defaultDisplayName, DateTimeOffset nowUtc, Guid? id = null)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalizedSlug) || !SlugPattern().IsMatch(normalizedSlug))
        {
            throw new ArgumentException("Service slug must be lowercase kebab-case (e.g. 'telegram').", nameof(slug));
        }

        var name = RequireDisplayName(defaultDisplayName);
        var service = new Service(id ?? Guid.CreateVersion7(), normalizedSlug, name);
        service.Raise(new ServiceUpserted(service.Id, service.Slug, nowUtc));
        return service;
    }

    public void Rename(string defaultDisplayName, DateTimeOffset nowUtc)
    {
        DefaultDisplayName = RequireDisplayName(defaultDisplayName);
        Raise(new ServiceUpserted(Id, Slug, nowUtc));
    }

    public void SetLocalizedNames(IEnumerable<LocalizedCatalogName> names)
    {
        _localizedNames.Clear();
        _localizedNames.AddRange(names.GroupBy(n => n.CultureCode, StringComparer.OrdinalIgnoreCase).Select(g => g.First()));
    }

    public void SetAvailability(bool enabled, DateTimeOffset nowUtc)
    {
        if (IsEnabled == enabled)
        {
            return;
        }

        IsEnabled = enabled;
        Raise(new CatalogAvailabilityChanged("service", Slug, enabled, nowUtc));
    }

    public void SetDisplayOrder(int order, DateTimeOffset nowUtc)
    {
        if (order < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(order), "Display order must be non-negative.");
        }

        if (DisplayOrder == order)
        {
            return;
        }

        DisplayOrder = order;
        Raise(new CatalogOrderChanged("service", Slug, order, nowUtc));
    }

    public string DisplayNameFor(string cultureCode) =>
        _localizedNames.FirstOrDefault(n => string.Equals(n.CultureCode, cultureCode, StringComparison.OrdinalIgnoreCase))?.DisplayName
        ?? DefaultDisplayName;

    [System.Text.RegularExpressions.GeneratedRegex(@"^[a-z0-9]+(-[a-z0-9]+)*$")]
    private static partial System.Text.RegularExpressions.Regex SlugPattern();

    private static string RequireDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Default display name is required.", nameof(displayName));
        }

        var name = displayName.Trim();
        if (name.Length > LocalizedCatalogName.MaxDisplayNameLength)
        {
            throw new ArgumentException($"Display name exceeds {LocalizedCatalogName.MaxDisplayNameLength} characters.", nameof(displayName));
        }

        return name;
    }
}
