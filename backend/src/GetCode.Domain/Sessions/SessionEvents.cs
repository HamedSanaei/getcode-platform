using GetCode.Domain.Common;

namespace GetCode.Domain.Sessions;

public sealed record SessionIssued(
    Guid SessionId,
    Guid UserId,
    string SiteKey,
    DateTimeOffset ExpiresAtUtc,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record SessionRevoked(
    Guid SessionId,
    Guid UserId,
    string SiteKey,
    string? Reason,
    DateTimeOffset OccurredAtUtc) : IDomainEvent;
