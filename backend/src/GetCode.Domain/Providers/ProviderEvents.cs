using GetCode.Domain.Common;

namespace GetCode.Domain.Providers;

public sealed record ProviderRegistered(Guid ProviderId, string ProviderKey, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ProviderAvailabilityChanged(string ProviderKey, bool Enabled, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record ProviderCapabilitiesChanged(string ProviderKey, bool SupportsActivation, bool SupportsRental, DateTimeOffset OccurredAtUtc) : IDomainEvent;
