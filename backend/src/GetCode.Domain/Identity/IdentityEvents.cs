using GetCode.Domain.Common;

namespace GetCode.Domain.Identity;

public sealed record UserRegistered(Guid UserId, string NormalizedEmail, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserAuthenticated(Guid UserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserTemporarilyLocked(Guid UserId, DateTimeOffset LockedUntilUtc, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserLockedPermanently(Guid UserId, string Reason, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserUnlocked(Guid UserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserDisabled(Guid UserId, string Reason, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record UserPasswordChanged(Guid UserId, DateTimeOffset OccurredAtUtc) : IDomainEvent;
