using GetCode.Application.Identity;
using GetCode.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Identity;

/// <summary>User persistence. Uniqueness of the normalized email is enforced by a database index.</summary>
internal sealed class UserRepository(GetCodeDbContext context) : IUserRepository
{
    public Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<bool> ExistsWithNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        context.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

    public void Add(User user) => context.Users.Add(user);
}

/// <summary>
/// Persists identity audit events in the same unit of work as the state change
/// that produced them. Metadata is structured and secret-free by contract.
/// </summary>
internal sealed class IdentityAuditTrail(GetCodeDbContext context) : IIdentityAuditTrail
{
    public async Task RecordAsync(IdentityAuditEvent auditEvent, CancellationToken cancellationToken)
    {
        foreach (var detailKey in auditEvent.Details?.Keys ?? Enumerable.Empty<string>())
        {
            if (LoggingRedaction.ForbiddenAuditKeys.Contains(detailKey))
            {
                throw new InvalidOperationException($"Audit details must not contain sensitive key '{detailKey}'.");
            }
        }

        context.IdentityAuditEvents.Add(IdentityAuditEventRecord.From(auditEvent));
        await context.SaveChangesAsync(cancellationToken);
    }
}
