using GetCode.Domain.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GetCode.Persistence.Authorization;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Key).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
        builder.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
        // jsonb primitive collection; membership semantics live in the aggregate.
        builder.PrimitiveCollection(x => x.Permissions).HasColumnType("jsonb");
        builder.Ignore(x => x.DomainEvents);
    }
}

/// <summary>Join row: a user's role assignment. No assignment means no permissions.</summary>
public sealed class UserRoleAssignmentRecord
{
    public Guid UserId { get; init; }
    public Guid RoleId { get; init; }
    public DateTimeOffset AssignedAtUtc { get; init; }

    private UserRoleAssignmentRecord()
    {
    }

    public static UserRoleAssignmentRecord Create(Guid userId, Guid roleId, DateTimeOffset assignedAtUtc) => new()
    {
        UserId = userId,
        RoleId = roleId,
        AssignedAtUtc = assignedAtUtc,
    };
}

internal sealed class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignmentRecord>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignmentRecord> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id");
        builder.Property(x => x.AssignedAtUtc).HasColumnName("assigned_at_utc");
        builder.HasIndex(x => x.UserId);
    }
}
