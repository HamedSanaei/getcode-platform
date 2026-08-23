namespace GetCode.Application.Providers;

/// <summary>
/// Port owned by GetCode. Provider adapters implement this contract; provider-specific DTOs and IDs stay in Infrastructure.
/// This is intentionally small and will evolve through provider-contract tasks.
/// </summary>
public interface IVirtualNumberProvider
{
    string ProviderKey { get; }

    Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(
        ProviderSearchQuery query,
        CancellationToken cancellationToken);

    Task<ProviderResult<ProviderReservation>> ReserveAsync(
        ProviderReservationRequest request,
        CancellationToken cancellationToken);

    Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(
        string providerOperationId,
        CancellationToken cancellationToken);

    Task<ProviderResult> CancelAsync(
        string providerOperationId,
        CancellationToken cancellationToken);
}

public sealed record ProviderSearchQuery(string CountryKey, string ServiceKey, string ProductTypeKey);

public sealed record ProviderOffer(
    string ProviderOfferKey,
    decimal CostAmount,
    string CostCurrency,
    bool IsAvailable,
    DateTimeOffset ObservedAtUtc);

public sealed record ProviderReservationRequest(
    string ProviderOfferKey,
    string IdempotencyKey,
    string CorrelationId);

/// <remarks>PhoneNumberE164 is sensitive application data. Never destructure/log this record wholesale.</remarks>
public sealed record ProviderReservation(
    string ProviderOperationId,
    string PhoneNumberE164,
    DateTimeOffset ReservedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public sealed record ProviderActivationSnapshot(
    string ProviderOperationId,
    ProviderActivationState State,
    bool HasMessage,
    DateTimeOffset ObservedAtUtc);

public enum ProviderActivationState
{
    Unknown = 0,
    Reserved = 1,
    WaitingForMessage = 2,
    MessageReceived = 3,
    Completed = 4,
    Cancelled = 5,
    Expired = 6,
    Failed = 7,
}

public enum ProviderErrorCode
{
    None = 0,
    Unavailable = 1,
    Timeout = 2,
    RateLimited = 3,
    InsufficientProviderBalance = 4,
    OfferUnavailable = 5,
    Rejected = 6,
    InvalidResponse = 7,
    AuthenticationFailed = 8,
    Unknown = 99,
}

public record ProviderResult(bool IsSuccess, ProviderErrorCode ErrorCode, string? SafeErrorCode)
{
    public static ProviderResult Success() => new(true, ProviderErrorCode.None, null);
    public static ProviderResult Failure(ProviderErrorCode code, string? safeErrorCode = null) => new(false, code, safeErrorCode);
}

public sealed record ProviderResult<T>(bool IsSuccess, T? Value, ProviderErrorCode ErrorCode, string? SafeErrorCode)
{
    public static ProviderResult<T> Success(T value) => new(true, value, ProviderErrorCode.None, null);
    public static ProviderResult<T> Failure(ProviderErrorCode code, string? safeErrorCode = null) => new(false, default, code, safeErrorCode);
}
