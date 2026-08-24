using GetCode.Domain.Sessions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Identity;

/// <summary>
/// M02-002 server-side sessions. Only SHA-256 token hashes are stored;
/// the unique index makes token lookup O(1) and prevents duplicate rows.
/// </summary>
internal sealed class SessionConfiguration : IEntityTypeConfiguration<Session>
{
    public void Configure(EntityTypeBuilder<Session> builder)
    {
        builder.ToTable("sessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.SiteKey).HasMaxLength(64).IsRequired();
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SiteKey });
        builder.Property(x => x.RevocationReason).HasMaxLength(128);
        builder.Property(x => x.RotatedFromSessionId);
        // Domain events are transient and never mapped.
        builder.Ignore(x => x.DomainEvents);
    }
}
