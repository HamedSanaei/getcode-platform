using GetCode.Domain.Common;

namespace GetCode.Domain.Authorization;

public sealed record RoleRegistered(Guid RoleId, string Key, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record RolePermissionsChanged(string RoleKey, IReadOnlyList<string> Permissions, DateTimeOffset OccurredAtUtc) : IDomainEvent;
