using GetCode.Domain.Authorization;
using GetCode.Domain.Catalog;
using GetCode.Domain.Identity;
using GetCode.Domain.Providers;
using GetCode.Domain.Wallets;
using GetCode.Persistence.Authorization;
using GetCode.Persistence.Identity;
using GetCode.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence;

public sealed class GetCodeDbContext(DbContextOptions<GetCodeDbContext> options) : DbContext(options)
{
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<User> Users => Set<User>();
    public DbSet<IdentityAuditEventRecord> IdentityAuditEvents => Set<IdentityAuditEventRecord>();
    public DbSet<Country> Countries => Set<Country>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<ProductSku> ProductSkus => Set<ProductSku>();
    public DbSet<ProviderDefinition> Providers => Set<ProviderDefinition>();
    public DbSet<ProviderMapping> ProviderMappings => Set<ProviderMapping>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRoleAssignmentRecord> UserRoles => Set<UserRoleAssignmentRecord>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);
        Conventions.NamingConventions.Apply(modelBuilder);
    }
}
