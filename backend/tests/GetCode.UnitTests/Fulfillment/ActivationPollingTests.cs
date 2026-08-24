using GetCode.Application.Fulfillment;
using GetCode.Application.Providers;
using GetCode.Domain.Orders;

namespace GetCode.UnitTests.Fulfillment;

/// <summary>
/// M07-003: bounded polling, deduplicated message receipt, idempotent
/// transitions, and OTP/raw-SMS redaction from logs.
/// </summary>
public sealed class ActivationPollingTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static Order ReservedOrder()
    {
        var order = new Order(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "idem-poll", 100m, "RUB",
            "RU", "telegram", "activation", 1, T0);
        order.MarkPaymentAuthorized();
        order.MarkPaid();
        order.StartFulfillment();
        order.MarkProviderReserved("op-42");
        return order;
    }

    private sealed class ScriptedProvider : IVirtualNumberProvider
    {
        public string ProviderKey => "scripted";
        public ProviderActivationSnapshot NextSnapshot { get; set; } =
            new("op-42", ProviderActivationState.WaitingForMessage, HasMessage: false, T0);
        public ProviderErrorCode? NextError { get; set; }
        public int Calls { get; private set; }

        public Task<ProviderResult<ProviderActivationSnapshot>> GetActivationAsync(string id, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(NextError is { } err
                ? ProviderResult<ProviderActivationSnapshot>.Failure(err, err == ProviderErrorCode.RateLimited ? "rate-limited" : "provider-error")
                : ProviderResult<ProviderActivationSnapshot>.Success(NextSnapshot));
        }

        public Task<ProviderResult<IReadOnlyCollection<ProviderOffer>>> SearchOffersAsync(ProviderSearchQuery q, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderResult<ProviderReservation>> ReserveAsync(ProviderReservationRequest r, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProviderResult> CancelAsync(string id, CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class ScriptedSmsReader : ISmsBodyReader
    {
        public string Body { get; set; } = "G-123456 is your code";
        public int Calls { get; private set; }
        public bool Fail { get; set; }

        public Task<ProviderResult<string>> ReadLatestMessageAsync(string id, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(Fail
                ? ProviderResult<string>.Failure(ProviderErrorCode.Unavailable, "transient-http")
                : ProviderResult<string>.Success(Body));
        }
    }

    [Theory]
    [InlineData(1, 10_000)]
    [InlineData(2, 20_000)]
    [InlineData(3, 40_000)]
    [InlineData(5, 120_000)] // 160s would exceed the 2min cap
    public void Poll_backoff_grows_but_is_capped(int pollNumber, double expectedMs)
    {
        Assert.Equal(expectedMs, ActivationPollingPolicy.DelayFor(pollNumber).TotalMilliseconds);
        Assert.True(ActivationPollingPolicy.DelayFor(30) <= ActivationPollingPolicy.MaxInterval); // capped
    }

    [Fact]
    public async Task Waiting_poll_leaves_state_untouched()
    {
        var provider = new ScriptedProvider();
        var reader = new ScriptedSmsReader();
        var service = new ActivationPollingService(provider, reader);
        var order = ReservedOrder();

        var outcome = await service.PollAsync(order, pollsTakenSoFar: 3, DateTimeOffset.UtcNow.AddMinutes(-1), TestContext.Current.CancellationToken);

        Assert.Equal(ActivationPollingService.PollOutcome.Waiting, outcome);
        Assert.Equal(OrderFulfillmentState.Reserved, order.FulfillmentState);
    }

    [Fact]
    public async Task Message_arrival_records_receipt_and_completes_exactly_once()
    {
        var provider = new ScriptedProvider { NextSnapshot = new("op-42", ProviderActivationState.MessageReceived, HasMessage: true, T0) };
        var reader = new ScriptedSmsReader();
        var service = new ActivationPollingService(provider, reader);
        var order = ReservedOrder();

        var first = await service.PollAsync(order, 1, DateTimeOffset.UtcNow.AddMinutes(-1), TestContext.Current.CancellationToken);
        Assert.Equal(ActivationPollingService.PollOutcome.MessageRecorded, first);
        Assert.Equal(OrderFulfillmentState.Completed, order.FulfillmentState);

        // Repeated polls after completion are idempotent no-ops (deduplicated).
        var second = await service.PollAsync(order, 2, DateTimeOffset.UtcNow.AddMinutes(-1), TestContext.Current.CancellationToken);
        Assert.Equal(ActivationPollingService.PollOutcome.CompletedAlready, second);
    }

    [Fact]
    public void Raw_sms_body_never_enters_log_summaries()
    {
        var secretBody = "G-482913 is your code";
        var summary = ActivationPollingService.SafeSummary(secretBody);

        Assert.DoesNotContain("482913", summary, StringComparison.Ordinal);
        Assert.DoesNotContain("code", summary);
        Assert.Contains("len=21", summary); // presence + length only

        Assert.Equal("sms:none", ActivationPollingService.SafeSummary(null));
        Assert.Equal("sms:none", ActivationPollingService.SafeSummary(""));
    }

    [Fact]
    public async Task Rate_limit_is_transient_not_an_error_storm()
    {
        var provider = new ScriptedProvider { NextError = ProviderErrorCode.RateLimited };
        var service = new ActivationPollingService(provider, new ScriptedSmsReader());
        var order = ReservedOrder();

        var outcome = await service.PollAsync(order, 4, DateTimeOffset.UtcNow.AddMinutes(-1), TestContext.Current.CancellationToken);

        Assert.Equal(ActivationPollingService.PollOutcome.RateLimited, outcome);
        Assert.Equal(OrderFulfillmentState.Reserved, order.FulfillmentState);
    }

    [Fact]
    public async Task Exhaustion_moves_to_manual_review_path()
    {
        var provider = new ScriptedProvider(); // forever waiting
        var service = new ActivationPollingService(provider, new ScriptedSmsReader());
        var order = ReservedOrder();

        var exhausted = await service.PollAsync(order, ActivationPollingPolicy.MaxPolls, T0, TestContext.Current.CancellationToken);
        Assert.Equal(ActivationPollingService.PollOutcome.Exhausted, exhausted);
    }
}
