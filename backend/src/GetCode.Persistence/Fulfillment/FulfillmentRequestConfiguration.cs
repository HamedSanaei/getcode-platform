using GetCode.Persistence.Fulfillment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Fulfillment;

public sealed class FulfillmentRequestConfiguration : IEntityTypeConfiguration<FulfillmentRequestRecord>
{
    public void Configure(EntityTypeBuilder<FulfillmentRequestRecord> builder)
    {
        builder.ToTable("fulfillment_requests");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.OrderId).HasColumnName("order_id");
        builder.Property(r => r.State).HasColumnName("state");
        builder.Property(r => r.LeaseOwner).HasColumnName("lease_owner").HasMaxLength(128);
        builder.Property(r => r.LeaseExpiresAtUtc).HasColumnName("lease_expires_at_utc");
        builder.Property(r => r.AttemptCount).HasColumnName("attempt_count");
        builder.Property(r => r.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.HasIndex(r => r.OrderId).IsUnique();
    }
}
