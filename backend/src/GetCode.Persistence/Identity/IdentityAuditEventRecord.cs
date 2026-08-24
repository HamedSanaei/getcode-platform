using GetCode.Domain.Identity;

namespace GetCode.Persistence.Identity;

public sealed class IdentityAuditEventRecord
{
    public Guid Id { get; init; }
    public DateTimeOffset OccurredAtUtc { get; init; }
    public Guid? UserId { get; init; }
    public required string EventType { get; init; }
    public bool Succeeded { get; init; }
    public string? CorrelationId { get; init; }
    public string? DetailsJson { get; init; }

    private IdentityAuditEventRecord()
    {
    }

    /// <summary>Maps an application audit event onto durable storage.</summary>
    public static IdentityAuditEventRecord From(GetCode.Application.Identity.IdentityAuditEvent auditEvent) => new()
    {
        Id = Guid.CreateVersion7(),
        OccurredAtUtc = auditEvent.OccurredAtUtc,
        UserId = auditEvent.UserId,
        EventType = auditEvent.EventType,
        Succeeded = auditEvent.Succeeded,
        CorrelationId = auditEvent.CorrelationId,
        DetailsJson = auditEvent.Details is null ? null : System.Text.Json.JsonSerializer.Serialize(auditEvent.Details),
    };
}
