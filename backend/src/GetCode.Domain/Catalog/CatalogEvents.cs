using GetCode.Domain.Common;

namespace GetCode.Domain.Catalog;

public sealed record CountryUpserted(Guid CountryId, string Code, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ServiceUpserted(Guid ServiceId, string Slug, DateTimeOffset OccurredAtUtc) : IDomainEvent;

/// <summary>Availability toggles are auditable admin actions across both catalog kinds.</summary>
public sealed record CatalogAvailabilityChanged(string Kind, string StableKey, bool Enabled, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record CatalogOrderChanged(string Kind, string StableKey, int DisplayOrder, DateTimeOffset OccurredAtUtc) : IDomainEvent;
