using System.Collections.Concurrent;
using GetCode.Application.Common;
using GetCode.Application.Providers;
using GetCode.Infrastructure.Common;

namespace GetCode.Infrastructure.Providers.Fake;

/// <summary>
/// Deterministic development/test adapter. It must never be enabled in production
/// (composition root wires it only for development).
/// <para>
/// Behavior is fully scripted by the caller: outcome queues per operation,
/// latency simulation, and pre-cancelled-token handling make it usable both for
/// manual runs and as the reference subject of the shared provider contract suite.
/// Real adapters are added under sibling folders and map provider-specific
/// contracts onto GetCode canonical contracts exactly like this one does.
/// </para>
/// </summary>
public sealed class FakeVirtualNumberProvider : IVirtualNumberProvider
{
    private readonly IClock _clock;
    private readonly ConcurrentQueue<ProviderResult<IReadOnlyCollection<ProviderOffer>>> _searchOutcomes = new();
    private readonly ConcurrentQueue<ProviderResult<ProviderReservation>> _reserveOutcomes = new();
    private readonly ConcurrentQueue<ProviderResult<ProviderActivationSnapshot>> _activationOutcomes = new();
    private readonly ConcurrentQueue<ProviderResult> _cancelOutcomes = new();
    private readonly ConcurrentDictionary<string, ProviderReservation> _reservations = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, ProviderActivationSnapshot> _activations = new(StringComparer.Ordinal);

    public FakeVirtualNumberProvider(IClock? clock = null)
    {
        _clock = clock ?? new SystemClock();
    }

    public string ProviderKey => "fake";

    /// <summary>Simulated latency applied to every call; honors the cancellation token.</summary>
    public TimeSpan Latency { get; set; } = TimeSpan.Zero;

    /// <summary>When true, every call observes the token first and throws OperationCanceledException.</summary>
    public bool HonorCancelledTokens { get; set; } = true;

    /// <summary>Default search outcome used when no explicit outcome is queued.</summary>
    public ProviderResult<IReadOnlyCollection<ProviderOffer>> DefaultSearchOutcome { get; set; } =
        ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(
        [
            new ProviderOffer("fake-offer-activation", 0.25m, "USD", true, Epoch),
            new ProviderOffer("fake-offer-rental", 1.10m, "USD", true, Epoch),
        ]);

    /// <summary>The phone number handed out by reservations (E.164 test value).</summary>
    public string ReservationPhoneNumber { get; set; } = "+15550000000";

    public TimeSpan ReservationLifetime { get; set; } = TimeSpan.FromMinutes(15);

    // Deterministic anchor so scripted outputs are reproducible in tests.
    private static DateTimeOffset Epoch => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public void QueueSearchOutcome(ProviderResult<IReadOnlyCollection<ProviderOffer>> outcome) => _searchOutcomes.Enqueue(outcome);
    public void QueueReserveOutcome(ProviderResult<ProviderReservation> outcome) => _reserveOutcomes.Enqueue(outcome);
    public void QueueActivationOutcome(ProviderResult<ProviderActivationSnapshot> outcome) => _activationOutcomes.Enqueue(outcome);
    public void QueueCancelOutcome(ProviderResult outcome) => _cancelOutcomes.Enqueue(outcome);

    public async Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(
        ProviderSearchQuery query,
        CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);

        var outcome = _searchOutcomes.TryDequeue(out var queued)
            ? queued
            : DefaultSearchOutcome;
        if (!outcome.IsSuccess && outcome.Value is null)
        {
            return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Failure(outcome.ErrorCode, outcome.SafeErrorCode);
        }

        return ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success(
        [.. outcome.Value!.Select(o => o with { ObservedAtUtc = _clock.UtcNow })]);
    }

    public async Task<ProviderResult<ProviderReservation>> ReserveAsync(
        ProviderReservationRequest request,
        CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);

        if (_reserveOutcomes.TryDequeue(out var queued))
        {
            return queued.IsSuccess ? Track(queued.Value!) : Failure(queued);
        }

        // Idempotency: same key replays the original reservation deterministically.
        var reservation = _reservations.GetOrAdd(request.IdempotencyKey, _ =>
            new ProviderReservation(
                $"fake-{request.IdempotencyKey}",
                ReservationPhoneNumber,
                _clock.UtcNow,
                _clock.UtcNow + ReservationLifetime));
        RegisterActivation(reservation.ProviderOperationId);
        return ProviderResult<ProviderReservation>.Success(reservation);
    }

    public async Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(
        string providerOperationId,
        CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);

        if (_activationOutcomes.TryDequeue(out var queued))
        {
            return queued.IsSuccess && queued.Value is null
                ? ProviderResult<ProviderActivationSnapshot>.Failure(queued.ErrorCode, queued.SafeErrorCode)
                : queued;
        }

        return _activations.TryGetValue(providerOperationId, out var snapshot)
            ? ProviderResult<ProviderActivationSnapshot>.Success(snapshot)
            : ProviderResult<ProviderActivationSnapshot>.Failure(ProviderErrorCode.OfferUnavailable, "unknown_operation");
    }

    public async Task<ProviderResult> CancelAsync(string providerOperationId, CancellationToken cancellationToken)
    {
        await DelayAsync(cancellationToken);

        if (_cancelOutcomes.TryDequeue(out var queued))
        {
            return queued;
        }

        if (!_activations.ContainsKey(providerOperationId))
        {
            return ProviderResult.Failure(ProviderErrorCode.OfferUnavailable, "unknown_operation");
        }

        _activations[providerOperationId] = _activations[providerOperationId] with
        {
            State = ProviderActivationState.Cancelled,
            ObservedAtUtc = _clock.UtcNow,
        };
        return ProviderResult.Success();
    }

    /// <summary>Test helper: registers a live reservation so status/cancel flows have a subject.</summary>
    public ProviderReservation SeedReservation(string providerOperationId, ProviderActivationState initialState = ProviderActivationState.WaitingForMessage)
    {
        var reservation = new ProviderReservation(providerOperationId, ReservationPhoneNumber, _clock.UtcNow, _clock.UtcNow + ReservationLifetime);
        _reservations[providerOperationId] = reservation;
        RegisterActivation(providerOperationId, initialState);
        return reservation;
    }

    private void RegisterActivation(string providerOperationId, ProviderActivationState state = ProviderActivationState.Reserved) =>
        _activations.GetOrAdd(
            providerOperationId,
            id => new ProviderActivationSnapshot(id, state, false, _clock.UtcNow));

    private ProviderResult<ProviderReservation> Track(ProviderReservation reservation)
    {
        _reservations[reservation.ProviderOperationId] = reservation;
        RegisterActivation(reservation.ProviderOperationId);
        return ProviderResult<ProviderReservation>.Success(reservation);
    }

    private static ProviderResult<ProviderReservation> Failure(ProviderResult<ProviderReservation> queued) =>
        ProviderResult<ProviderReservation>.Failure(queued.ErrorCode, queued.SafeErrorCode);

    private async Task DelayAsync(CancellationToken cancellationToken)
    {
        if (HonorCancelledTokens)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }

        if (Latency > TimeSpan.Zero)
        {
            await Task.Delay(Latency, cancellationToken);
        }
    }
}
