using GetCode.Domain.Common;

namespace GetCode.Domain.Catalog;

public sealed record ProductSkuUpserted(Guid ProductSkuId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ProductSkuAvailabilityChanged(Guid ProductSkuId, bool Offered, DateTimeOffset OccurredAtUtc) : IDomainEvent;
