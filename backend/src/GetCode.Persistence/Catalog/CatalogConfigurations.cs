using GetCode.Domain.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Catalog;

internal sealed class CountryConfiguration : IEntityTypeConfiguration<Country>
{
    public void Configure(EntityTypeBuilder<Country> builder)
    {
        builder.ToTable("countries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).HasMaxLength(2).IsFixedLength().IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.DefaultDisplayName).HasMaxLength(LocalizedCatalogName.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        // Localized display metadata is owned by GetCode, stored as a child collection.
        builder.OwnsMany(x => x.LocalizedNames, names =>
        {
            names.ToTable("country_localized_names");
            names.WithOwner().HasForeignKey("country_id");
            names.Property(n => n.CultureCode).HasColumnName("culture_code").HasMaxLength(10).IsRequired();
            names.Property(n => n.DisplayName).HasColumnName("display_name").HasMaxLength(LocalizedCatalogName.MaxDisplayNameLength).IsRequired();
            names.HasKey("country_id", nameof(LocalizedCatalogName.CultureCode));
        });
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("services");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Slug).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.DefaultDisplayName).HasMaxLength(LocalizedCatalogName.MaxDisplayNameLength).IsRequired();
        builder.Property(x => x.DisplayOrder).IsRequired();
        builder.OwnsMany(x => x.LocalizedNames, names =>
        {
            names.ToTable("service_localized_names");
            names.WithOwner().HasForeignKey("service_id");
            names.Property(n => n.CultureCode).HasColumnName("culture_code").HasMaxLength(10).IsRequired();
            names.Property(n => n.DisplayName).HasColumnName("display_name").HasMaxLength(LocalizedCatalogName.MaxDisplayNameLength).IsRequired();
            names.HasKey("service_id", nameof(LocalizedCatalogName.CultureCode));
        });
        builder.Ignore(x => x.DomainEvents);
    }
}
