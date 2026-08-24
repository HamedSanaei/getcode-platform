using System.Text.Json;
using GetCode.Application.Providers;

namespace GetCode.ProviderContractTests;

/// <summary>
/// M04-001: the shared behavioral contract every <see cref="IVirtualNumberProvider"/>
/// adapter must satisfy — search/reserve/status/cancel happy paths, error and
/// timeout semantics, idempotent reservation, cancellation-token observance,
/// and a leakage guard proving raw vendor payloads never reach canonical results.
/// <para>
/// Concrete adapters (fake today, real providers in M04-002+) subclass this with
/// their own factory, so one suite keeps every adapter honest.
/// </para>
/// </summary>
public abstract class VirtualNumberProviderContractTests
{
    /// <summary>Factory hook: return a fresh adapter instance per test.</summary>
    protected abstract Task<IVirtualNumberProvider> CreateProviderAsync();

    /// <summary>Offer key used across scenarios; adapters may need vendor-shaped keys.</summary>
    protected virtual string OfferKey => "offer-1";

    [Fact]
    public async Task SearchOffers_returns_offers_with_canonical_shape_and_sane_timestamps()
    {
        var provider = await CreateProviderAsync();
        var result = await provider.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), TestContext.Current.CancellationToken);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.NotEmpty(result.Value!);
        Assert.Null(result.SafeErrorCode); // success carries no error token

        foreach (var offer in result.Value!)
        {
            Assert.False(string.IsNullOrWhiteSpace(offer.ProviderOfferKey));
            Assert.True(offer.CostAmount >= 0m, "cost is non-negative");
            Assert.False(string.IsNullOrWhiteSpace(offer.CostCurrency));
            Assert.InRange(offer.ObservedAtUtc.DateTime, DateTime.UtcNow.AddMinutes(-5), DateTime.UtcNow.AddMinutes(5));
        }
    }

    [Fact]
    public async Task SearchOffers_reports_unavailability_as_result_not_exception()
    {
        var provider = await CreateProviderAsync();

        // Adapter-specific unavailability: empty offer set is a legitimate success.
        if (provider is Infrastructure.Providers.Fake.FakeVirtualNumberProvider fake)
        {
            fake.QueueSearchOutcome(ProviderResult<IReadOnlyCollection<ProviderOffer>>.Success([]));
        }

        var result = await provider.SearchOffersAsync(new ProviderSearchQuery("XX", "unknown-service", "activation"), TestContext.Current.CancellationToken);

        // Never throws; either a success (possibly empty) or a failure with a known, safe error code.
        if (!result.IsSuccess)
        {
            Assert.True(IsKnownErrorCode(result.ErrorCode), $"unexpected error code {result.ErrorCode}");
            Assert.False(string.IsNullOrWhiteSpace(result.SafeErrorCode), "failures must carry a safe, stable error token");
        }
    }

    [Fact]
    public async Task Reserve_is_idempotent_per_idempotency_key()
    {
        var provider = await CreateProviderAsync();

        var first = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, "idem-key-1", "corr-1"), TestContext.Current.CancellationToken);
        var replay = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, "idem-key-1", "corr-1"), TestContext.Current.CancellationToken);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value!.ProviderOperationId, replay.Value!.ProviderOperationId);
        Assert.Equal(first.Value!.PhoneNumberE164, replay.Value!.PhoneNumberE164);
        Assert.Matches(@"^\+\d{8,15}$", first.Value!.PhoneNumberE164); // E.164 shape

        var differentKey = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, "idem-key-2", "corr-1"), TestContext.Current.CancellationToken);
        Assert.NotEqual(first.Value!.ProviderOperationId, differentKey.Value!.ProviderOperationId);

        Assert.InRange(
            first.Value!.ExpiresAtUtc?.DateTime ?? DateTime.MaxValue,
            first.Value!.ReservedAtUtc.UtcDateTime,
            DateTime.UtcNow.AddHours(24));
    }

    [Fact]
    public async Task GetActivation_reports_known_operation_and_safe_failure_for_unknown()
    {
        var provider = await CreateProviderAsync();
        var reserved = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, $"status-{Guid.NewGuid():N}", "corr"), TestContext.Current.CancellationToken);
        Assert.True(reserved.IsSuccess);

        var snapshot = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);
        Assert.True(snapshot.IsSuccess);
        Assert.Equal(reserved.Value!.ProviderOperationId, snapshot.Value!.ProviderOperationId);
        Assert.True(Enum.IsDefined(snapshot.Value!.State));

        var unknown = await provider.GetActivationAsync($"does-not-exist-{Guid.NewGuid():N}", TestContext.Current.CancellationToken);
        Assert.False(unknown.IsSuccess);
        Assert.True(IsKnownErrorCode(unknown.ErrorCode));
        Assert.False(string.IsNullOrWhiteSpace(unknown.SafeErrorCode));
    }

    [Fact]
    public async Task Cancel_transitions_reservation_to_cancelled_state()
    {
        var provider = await CreateProviderAsync();
        var reserved = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, $"cancel-{Guid.NewGuid():N}", "corr"), TestContext.Current.CancellationToken);
        Assert.True(reserved.IsSuccess);

        var cancelled = await provider.CancelAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);
        Assert.True(cancelled.IsSuccess);

        var afterCancel = await provider.GetActivationAsync(reserved.Value!.ProviderOperationId, TestContext.Current.CancellationToken);
        Assert.True(afterCancel.IsSuccess);
        Assert.Equal(ProviderActivationState.Cancelled, afterCancel.Value!.State);
    }

    [Fact]
    public async Task Cancel_of_unknown_operation_is_a_safe_failure()
    {
        var provider = await CreateProviderAsync();

        var result = await provider.CancelAsync($"nope-{Guid.NewGuid():N}", TestContext.Current.CancellationToken);

        Assert.False(result.IsSuccess);
        Assert.True(IsKnownErrorCode(result.ErrorCode));
    }

    [Fact]
    public async Task Adapters_observe_cancellation_tokens()
    {
        var provider = await CreateProviderAsync();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // The adapter must surface cancellation promptly via the standard exception type.
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            provider.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), cts.Token));
    }

    /// <summary>
    /// Leakage guard: whatever an adapter returns, the serialized canonical
    /// result contains only contract fields. Raw vendor payloads smuggled into
    /// extra properties or strings fail here.
    /// </summary>
    [Fact]
    public async Task Canonical_results_never_expose_raw_vendor_payload_fields()
    {
        var provider = await CreateProviderAsync();
        var search = await provider.SearchOffersAsync(new ProviderSearchQuery("IR", "telegram", "activation"), TestContext.Current.CancellationToken);
        var reserve = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, $"leak-{Guid.NewGuid():N}", "corr"), TestContext.Current.CancellationToken);
        Assert.True(search.IsSuccess);
        Assert.True(reserve.IsSuccess);

        var allowedOfferFields = new HashSet<string>(StringComparer.Ordinal) { "providerOfferKey", "costAmount", "costCurrency", "isAvailable", "observedAtUtc" };
        var allowedReservationFields = new HashSet<string>(StringComparer.Ordinal) { "providerOperationId", "phoneNumberE164", "reservedAtUtc", "expiresAtUtc" };

        var searchJson = JsonSerializer.Serialize(search.Value, JsonOptions);
        var searchKeys = ExtractTopLevelElementKeys(searchJson);
        Assert.Subset(searchKeys, allowedOfferFields);

        var reservationJson = JsonSerializer.Serialize(reserve.Value, JsonOptions);
        Assert.Subset(ExtractTopLevelElementKeys(reservationJson), allowedReservationFields);

        // Success results never carry error tokens.
        Assert.Null(search.SafeErrorCode);
        Assert.Null(reserve.SafeErrorCode);
    }

    private static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);

    private static HashSet<string> ExtractTopLevelElementKeys(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ValueKind switch
        {
            JsonValueKind.Array => [.. document.RootElement.EnumerateArray().SelectMany(e => e.EnumerateObject().Select(p => p.Name))],
            _ => [.. document.RootElement.EnumerateObject().Select(p => p.Name)],
        };
    }

    private static bool IsKnownErrorCode(ProviderErrorCode code) =>
        code != ProviderErrorCode.None && Enum.IsDefined(code);

    [Fact]
    public async Task Timeout_simulation_maps_to_timeout_error_code()
    {
        var provider = await CreateProviderAsync();

        if (provider is Infrastructure.Providers.Fake.FakeVirtualNumberProvider fake)
        {
            fake.QueueReserveOutcome(ProviderResult<ProviderReservation>.Failure(ProviderErrorCode.Timeout, "upstream_timeout"));
        }
        else
        {
            return; // real-adapter timeout behavior gets its own scripted test in its subclass
        }

        var result = await provider.ReserveAsync(new ProviderReservationRequest(OfferKey, $"timeout-{Guid.NewGuid():N}", "corr"), TestContext.Current.CancellationToken);
        Assert.False(result.IsSuccess);
        Assert.Equal(ProviderErrorCode.Timeout, result.ErrorCode);
        Assert.Matches("^[A-Za-z0-9_.\\-]{0,64}$", result.SafeErrorCode); // stable token, not a raw payload
    }
}
