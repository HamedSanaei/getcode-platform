using GetCode.Domain.Common;

namespace GetCode.Domain.Wallets;

public sealed record WalletOpened(Guid WalletId, Guid OwnerUserId, string Currency, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record WalletCredited(Guid WalletId, long AmountMinor, long BalanceAfterMinor, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record WalletDebited(Guid WalletId, long AmountMinor, long BalanceAfterMinor, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record WalletAdjusted(Guid WalletId, long AmountMinor, long BalanceAfterMinor, DateTimeOffset OccurredAtUtc) : IDomainEvent;

public sealed record WalletClosed(Guid WalletId, long BalanceMinorAtClose, DateTimeOffset OccurredAtUtc) : IDomainEvent;
