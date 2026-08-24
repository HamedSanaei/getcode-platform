using GetCode.Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Orders;

/// <summary>M06-002: order mapping — the (customer, idempotency key) pair is the durable duplicate shield.</summary>
public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("order_id");
        builder.Property(o => o.CustomerId).HasColumnName("customer_id");
        builder.Property(o => o.QuoteId).HasColumnName("quote_id");
        builder.Property(o => o.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(128);
        builder.Property(o => o.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(o => o.Currency).HasColumnName("currency").HasMaxLength(3);
        builder.Property(o => o.CountryKey).HasColumnName("country_key").HasMaxLength(64);
        builder.Property(o => o.ServiceKey).HasColumnName("service_key").HasMaxLength(64);
        builder.Property(o => o.ProductTypeKey).HasColumnName("product_type_key").HasMaxLength(32);
        builder.Property(o => o.PricingRuleVersion).HasColumnName("pricing_rule_version");
        builder.Property(o => o.CreatedAtUtc).HasColumnName("created_at_utc");
        builder.Property(o => o.PaymentState).HasColumnName("payment_state");
        builder.Property(o => o.FulfillmentState).HasColumnName("fulfillment_state");
        builder.Property(o => o.ProviderOperationId).HasColumnName("provider_operation_id").HasMaxLength(128);

        builder.HasIndex(o => new { o.CustomerId, o.IdempotencyKey }).IsUnique();
    }
}
