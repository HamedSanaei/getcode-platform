using GetCode.Domain.Catalog;
using GetCode.Domain.Identity;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AssemblyMarker).Assembly);
        Conventions.NamingConventions.Apply(modelBuilder);
    }
}
