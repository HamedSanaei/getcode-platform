using GetCode.Application.Providers;

namespace GetCode.Infrastructure.Providers.Fake;

/// <summary>
/// Deterministic development/test adapter. It must never be enabled in production.
/// Real provider adapters are added under sibling folders and map provider-specific contracts to GetCode canonical contracts.
/// </summary>
internal sealed class FakeVirtualNumberProvider : IVirtualNumberProvider
{
    public string ProviderKey => "fake";

    public Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(
        ProviderSearchQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ProviderOffer> offers =
        [
            new ProviderOffer("fake-offer", 0.25m, "USD", true, DateTimeOffset.UtcNow),
        ];
        return Task.FromResult(ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(offers));
    }

    public Task<ProviderResult<ProviderReservation>> ReserveAsync(
        ProviderReservationRequest request,
        CancellationToken cancellationToken)
    {
        var reservation = new ProviderReservation(
            $"fake-{Guid.NewGuid():N}",
            "+15550000000",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(15));
        return Task.FromResult(ProviderResult<ProviderReservation>.Success(reservation));
    }

    public Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(
        string providerOperationId,
        CancellationToken cancellationToken)
    {
        var snapshot = new ProviderActivationSnapshot(
            providerOperationId,
            ProviderActivationState.WaitingForMessage,
            false,
            DateTimeOffset.UtcNow);
        return Task.FromResult(ProviderResult<ProviderActivationSnapshot>.Success(snapshot));
    }

    public Task<ProviderResult> CancelAsync(string providerOperationId, CancellationToken cancellationToken) =>
        Task.FromResult(ProviderResult.Success());
}
