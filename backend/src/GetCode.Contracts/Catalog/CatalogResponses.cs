namespace GetCode.Contracts.Catalog;

/// <summary>Paged envelope for public catalog reads.</summary>
public sealed record CatalogPageResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public sealed record CountryResponse(string StableKey, string DisplayName, int DisplayOrder);

public sealed record ServiceResponse(string StableKey, string DisplayName, int DisplayOrder);

/// <summary>A sellable offering. Deliberately provider-free: routing is internal.</summary>
public sealed record OfferResponse(string StableKey, string CountryCode, string ServiceSlug, string CountryName, string ServiceName, string ProductType);
