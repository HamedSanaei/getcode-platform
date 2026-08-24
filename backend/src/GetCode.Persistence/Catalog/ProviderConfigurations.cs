using GetCode.Domain.Providers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Catalog;

internal sealed class ProviderDefinitionConfiguration : IEntityTypeConfiguration<ProviderDefinition>
{
    public void Configure(EntityTypeBuilder<ProviderDefinition> builder)
    {
        builder.ToTable("providers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProviderKey).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.ProviderKey).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class ProviderMappingConfiguration : IEntityTypeConfiguration<ProviderMapping>
{
    public void Configure(EntityTypeBuilder<ProviderMapping> builder)
    {
        builder.ToTable("provider_mappings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ExternalCode).HasMaxLength(ProviderMapping.MaxExternalCodeLength).IsRequired();
        builder.Property(x => x.Kind).IsRequired();
        // One external code per provider/kind; the canonical target may be re-pointed.
        builder.HasIndex(x => new { x.ProviderId, x.Kind, x.ExternalCode }).IsUnique();
        builder.HasOne<ProviderDefinition>().WithMany().HasForeignKey(x => x.ProviderId);
        // CanonicalId is a polymorphic reference (country or service depending on Kind).
        // Referential integrity is enforced at the application layer, where Bind
        // resolves the canonical stable key through its repository before writing.
    }
}
