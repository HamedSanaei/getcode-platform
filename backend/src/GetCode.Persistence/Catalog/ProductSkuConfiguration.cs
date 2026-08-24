using GetCode.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Catalog;

internal sealed class ProductSkuConfiguration : IEntityTypeConfiguration<ProductSku>
{
    public void Configure(EntityTypeBuilder<ProductSku> builder)
    {
        builder.ToTable("product_skus");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CountryId).IsRequired();
        builder.Property(x => x.ServiceId).IsRequired();
        builder.Property(x => x.ProductType).IsRequired();
        // The canonical identity triple is unique; provider routing never lives here.
        builder.HasIndex(x => new { x.CountryId, x.ServiceId, x.ProductType }).IsUnique();
        builder.HasOne<Country>().WithMany().HasForeignKey(x => x.CountryId);
        builder.HasOne<Service>().WithMany().HasForeignKey(x => x.ServiceId);
        builder.Ignore(x => x.DomainEvents);
    }
}
