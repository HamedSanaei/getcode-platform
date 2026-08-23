namespace GetCode.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public required string Type { get; init; }
    public required string PayloadJson { get; init; }
    public string? CorrelationId { get; init; }
    public DateTimeOffset? ProcessedAtUtc { get; set; }
    public int AttemptCount { get; set; }
    public string? LastErrorCode { get; set; }
}
