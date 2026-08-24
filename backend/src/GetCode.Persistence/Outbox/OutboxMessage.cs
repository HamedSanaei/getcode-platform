using GetCode.Application.Common;

namespace GetCode.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public required string Type { get; init; }
    public required string PayloadJson { get; init; }
    public string? CorrelationId { get; init; }

    /// <summary>M00-008: W3C trace context captured at publish time (nullable for legacy rows).</summary>
    public string? TraceId { get; init; }
    public string? SpanId { get; init; }

    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorCode { get; set; }

    /// <summary>
    /// Creates an outbox message stamped with the current ambient trace/correlation
    /// context so the durable job that consumes it can join the originating workflow.
    /// </summary>
    public static OutboxMessage Create(string type, string payloadJson, string? correlationId = null, DateTimeOffset? occurredAtUtc = null)
    {
        var trace = GetCodeObservability.CaptureTraceContext();
        return new OutboxMessage
        {
            Id = Guid.CreateVersion7(),
            OccurredAtUtc = occurredAtUtc ?? DateTimeOffset.UtcNow,
            Type = type,
            PayloadJson = payloadJson,
            CorrelationId = correlationId,
            TraceId = trace.TraceId,
            SpanId = trace.SpanId,
        };
    }
}
