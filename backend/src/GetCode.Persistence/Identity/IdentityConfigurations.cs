using GetCode.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Identity;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.NormalizedEmail).HasMaxLength(320).IsRequired();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique();
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.LockReason).HasMaxLength(256);
        // Domain events are transient and never mapped.
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class IdentityAuditEventRecordConfiguration : IEntityTypeConfiguration<IdentityAuditEventRecord>
{
    public void Configure(EntityTypeBuilder<IdentityAuditEventRecord> builder)
    {
        builder.ToTable("identity_audit_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.EventType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CorrelationId).HasMaxLength(128);
        builder.Property(x => x.DetailsJson).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.UserId, x.OccurredAtUtc });
        builder.HasIndex(x => x.EventType);
    }
}
