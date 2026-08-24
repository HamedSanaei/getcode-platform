using GetCode.Application.Providers;

namespace GetCode.UnitTests.Providers;

/// <summary>
/// M04-003: worker-scheduling policy (healthy interval, exponential backoff,
/// cap, reset) and health-observation behavior including provider faults.
/// </summary>
public sealed class ProviderHealthAndPollingTests
{
    // ---- worker scheduling tests ------------------------------------------------

    [Fact]
    public void Healthy_providers_poll_at_the_fixed_interval()
    {
        Assert.Equal(TimeSpan.FromSeconds(60), ProviderPollingPolicy.NextDelay(0));
    }

    [Fact]
    public void Consecutive_failures_double_the_backoff_up_to_the_cap()
    {
        Assert.Equal(TimeSpan.FromSeconds(120), ProviderPollingPolicy.NextDelay(1));
        Assert.Equal(TimeSpan.FromSeconds(240), ProviderPollingPolicy.NextDelay(2));
        Assert.Equal(TimeSpan.FromSeconds(480), ProviderPollingPolicy.NextDelay(3));

        // Cap reached and never exceeded, no matter how long the outage.
        Assert.Equal(ProviderPollingPolicy.MaxBackoff, ProviderPollingPolicy.NextDelay(4 + 1));
        Assert.Equal(ProviderPollingPolicy.MaxBackoff, ProviderPollingPolicy.NextDelay(int.MaxValue / 2));
    }

    [Fact]
    public async Task First_success_after_failures_resets_the_schedule()
    {
        var service = new ProviderHealthService();
        var observer = new FakeObserver("prov-a");

        await service.ObserveAsync(observer, TestContext.Current.CancellationToken);
        observer.QueueFailure(ProviderErrorCode.Unavailable, "transient-http");
        var degraded = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);
        Assert.Equal(1, degraded.ConsecutiveFailures);
        Assert.True(ProviderPollingPolicy.NextDelay(degraded.ConsecutiveFailures) > ProviderPollingPolicy.HealthyInterval);

        var healthy = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);
        Assert.Equal(0, healthy.ConsecutiveFailures); // reset → schedule back to HealthyInterval
    }

    // ---- provider fault tests ---------------------------------------------------

    [Fact]
    public async Task Successful_observation_stores_a_normalized_timestamped_snapshot()
    {
        var service = new ProviderHealthService();

        var snapshot = await service.ObserveAsync(new FakeObserver("prov-ok", balance: 42.5m), TestContext.Current.CancellationToken);

        Assert.Equal(ProviderObservationOutcome.Healthy, snapshot.Outcome);
        Assert.Equal(42.5m, snapshot.BalanceAmount);
        Assert.Null(snapshot.SafeErrorToken);
        Assert.True(DateTimeOffset.UtcNow - snapshot.ObservedAtUtc < TimeSpan.FromSeconds(5));
        Assert.Single(service.LatestSnapshots);
    }

    [Fact]
    public async Task Faults_increment_failure_streak_and_degrade_then_mark_unreachable()
    {
        var service = new ProviderHealthService();
        var observer = new FakeObserver("prov-fault");

        observer.QueueFailure(ProviderErrorCode.Unavailable, "transient-http");
        observer.QueueFailure(ProviderErrorCode.Unavailable, "transient-http");
        var first = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);
        var second = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);
        observer.QueueFailure(ProviderErrorCode.Unavailable, "transient-http");
        var third = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);

        Assert.Equal(ProviderObservationOutcome.Degraded, first.Outcome);
        Assert.Equal("transient-http", first.SafeErrorToken);
        Assert.Null(first.BalanceAmount);
        Assert.Equal(2, second.ConsecutiveFailures);
        Assert.Equal(ProviderObservationOutcome.Unreachable, third.Outcome);
        Assert.Equal(3, third.ConsecutiveFailures);
    }

    [Fact]
    public async Task Throwing_observer_never_kills_polling_and_records_fault()
    {
        var service = new ProviderHealthService();
        var observer = new ThrowingObserver("prov-boom");

        var snapshot = await service.ObserveAsync(observer, TestContext.Current.CancellationToken);

        Assert.Equal(ProviderObservationOutcome.Degraded, snapshot.Outcome);
        Assert.Equal("observer-exception", snapshot.SafeErrorToken);
    }

    [Fact]
    public async Task Balance_is_supplier_telemetry_and_never_touches_wallet_state()
    {
        // The observation pipeline only produces read-model snapshots; there is
        // deliberately no wallet/ledger API it could feed. Pin the shape:
        var service = new ProviderHealthService();
        var snapshot = await service.ObserveAsync(new FakeObserver("prov-wallet"), TestContext.Current.CancellationToken);

        Assert.IsType<ProviderHealthSnapshot>(snapshot);
        Assert.IsAssignableFrom<decimal?>(snapshot.BalanceAmount);
    }

    private sealed class FakeObserver(string key, decimal balance = 0m) : IProviderBalanceObserver
    {
        private readonly Queue<ProviderResult<decimal>> _outcomes = new();

        public string ObserverProviderKey => key;

        public void QueueFailure(ProviderErrorCode code, string token) =>
            _outcomes.Enqueue(ProviderResult<decimal>.Failure(code, token));

        public Task<ProviderResult<decimal>> ObserveBalanceAsync(CancellationToken cancellationToken)
        {
            if (_outcomes.Count > 0)
            {
                return Task.FromResult(_outcomes.Dequeue());
            }

            return Task.FromResult(ProviderResult<decimal>.Success(balance));
        }
    }

    private sealed class ThrowingObserver(string key) : IProviderBalanceObserver
    {
        public string ObserverProviderKey => key;

        public Task<ProviderResult<decimal>> ObserveBalanceAsync(CancellationToken cancellationToken) =>
            throw new InvalidOperationException("adapter exploded");
    }
}
