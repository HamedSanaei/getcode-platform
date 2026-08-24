using System.Diagnostics;
using GetCode.Application.Common;
using Xunit;

namespace GetCode.UnitTests;

/// <summary>
/// M00-008 metric/activity naming review: the shared observability entry
/// points must follow the documented OpenTelemetry-compatible conventions so
/// future exporters can filter/route without special cases.
/// </summary>
public sealed class ObservabilityNamingTests
{
    [Fact]
    public void Instrumentation_names_use_the_documented_convention()
    {
        Assert.Matches(@"^GetCode\.[A-Z][A-Za-z]*$", GetCodeObservability.CoreActivitySourceName);
        Assert.Matches(@"^GetCode\.[A-Z][A-Za-z]*$", GetCodeObservability.CoreMeterName);
    }

    [Fact]
    public void Activity_sources_are_registered_with_expected_identity()
    {
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == GetCodeObservability.CoreActivitySourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);

        using var activity = GetCodeObservability.CoreActivitySource.StartActivity("order.paid");
        Assert.NotNull(activity);
        Assert.Equal(ActivityIdFormat.W3C, activity.IdFormat);
        Assert.Equal("order.paid", activity.OperationName);
    }

    [Fact]
    public void Captured_trace_context_is_empty_without_an_ambient_activity()
    {
        // Arrange an environment with no ambient activity for this async flow.
        var previous = Activity.Current;
        Activity.Current = null;
        try
        {
            var snapshot = GetCodeObservability.CaptureTraceContext();
            Assert.False(snapshot.HasContext);
            Assert.Null(snapshot.TraceId);
        }
        finally
        {
            Activity.Current = previous;
        }
    }

    [Fact]
    public void Captured_trace_context_carries_w3c_identifiers()
    {
        using var activity = new Activity("naming.probe");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        try
        {
            var snapshot = GetCodeObservability.CaptureTraceContext();
            Assert.True(snapshot.HasContext);
            Assert.Equal(activity.TraceId.ToString(), snapshot.TraceId);
            Assert.Equal(activity.SpanId.ToString(), snapshot.SpanId);
            Assert.Matches(@"^[0-9a-f]{32}$", snapshot.TraceId);
            Assert.Matches(@"^[0-9a-f]{16}$", snapshot.SpanId);
        }
        finally
        {
            activity.Stop();
        }
    }
}
