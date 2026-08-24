using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Outbox;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Type).HasMaxLength(500).IsRequired();
        builder.Property(x => x.PayloadJson).HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.TraceId).HasMaxLength(32);
        builder.Property(x => x.SpanId).HasMaxLength(16);
        builder.Property(x => x.LastErrorCode).HasMaxLength(200);
        builder.HasIndex(x => new { x.ProcessedAtUtc, x.OccurredAtUtc });
    }
}
