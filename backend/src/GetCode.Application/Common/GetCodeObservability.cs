using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace GetCode.Application.Common;

/// <summary>
/// M00-008 tracing/metrics foundation (OpenTelemetry-compatible).
///
/// Conventions (docs/architecture/OBSERVABILITY.md):
/// - one <see cref="ActivitySource"/> and one <see cref="Meter"/> per service area,
///   named <c>GetCode.&lt;Area&gt;</c> ("GetCode.Core" is the shared root);
/// - activities use the stable dotted event names from the observability docs
///   (e.g. <c>order.paid</c>, <c>provider.reserve.started</c>);
/// - metric instrument names are <c>getcode.&lt;area&gt;.&lt;noun&gt;</c> with explicit units;
/// - tags must have bounded cardinality: canonical keys, normalized error codes,
///   HTTP statuses, durations. Never user IDs, phone numbers, emails, provider
///   request bodies or other unbounded/sensitive values (those belong on log
///   events as structured properties, where redaction applies).
/// </summary>
public static class GetCodeObservability
{
    public const string CoreActivitySourceName = "GetCode.Core";
    public const string CoreMeterName = "GetCode.Core";

    public static readonly ActivitySource CoreActivitySource = new(CoreActivitySourceName, null);

    public static readonly Meter CoreMeter = new(CoreMeterName, null);

    /// <summary>
    /// Captures the ambient W3C trace context so durable records (outbox rows,
    /// job metadata) can reference the workflow that produced them even though
    /// the consumer runs later in another process.
    /// </summary>
    public static TraceContextSnapshot CaptureTraceContext() =>
        new(
            TraceId: Activity.Current?.TraceId.ToString(),
            SpanId: Activity.Current?.SpanId.ToString());
}

public sealed record TraceContextSnapshot(string? TraceId, string? SpanId)
{
    public bool HasContext => !string.IsNullOrEmpty(TraceId) && !string.IsNullOrEmpty(SpanId);
}
