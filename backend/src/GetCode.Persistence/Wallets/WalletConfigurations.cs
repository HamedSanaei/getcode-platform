using GetCode.Domain.Wallets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Wallets;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OwnerUserId).HasColumnName("owner_user_id");
        builder.HasIndex(x => new { x.OwnerUserId, x.Currency }).IsUnique();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.OpenedAtUtc).HasColumnName("opened_at_utc");
        builder.Property(x => x.BalanceMinor).HasColumnName("balance_minor");
        // Optimistic concurrency: Npgsql system column; conflicting writers get DbUpdateConcurrencyException.
        builder.Property(x => x.Version).HasColumnName("xmin").IsRowVersion();
        builder.Property(x => x.IsClosed).HasColumnName("is_closed");
        builder.Ignore(x => x.DomainEvents);
    }
}

internal sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("wallet_entries");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever().HasColumnName("id");
        builder.Property(x => x.WalletId).HasColumnName("wallet_id");
        builder.HasIndex(x => x.WalletId).HasMethod("btree");
        builder.Property(x => x.EntryType).HasColumnName("entry_type");
        builder.Property(x => x.AmountMinor).HasColumnName("amount_minor");
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(64).HasColumnName("reference_type");
        builder.Property(x => x.ReferenceId).HasColumnName("reference_id");
        // The idempotency identity of every mutation: enforced unique by the database.
        builder.HasIndex(x => x.IdempotencyKey).IsUnique();
        builder.Property(x => x.ResultingBalanceMinor).HasColumnName("resulting_balance_minor");
        builder.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");

        builder
            .HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(x => x.WalletId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
