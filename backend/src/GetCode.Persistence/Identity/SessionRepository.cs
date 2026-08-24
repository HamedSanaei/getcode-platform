using GetCode.Application.Identity;
using GetCode.Domain.Sessions;
using Microsoft.EntityFrameworkCore;

namespace GetCode.Persistence.Identity;

/// <summary>Session persistence. Token lookup is by hash; plaintext never lands here.</summary>
internal sealed class SessionRepository(GetCodeDbContext context) : ISessionRepository
{
    public Task<Session?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken) =>
        context.Sessions.FirstOrDefaultAsync(s => s.TokenHash == tokenHash, cancellationToken);

    public Task<Session?> FindByIdAsync(Guid sessionId, CancellationToken cancellationToken) =>
        context.Sessions.FirstOrDefaultAsync(s => s.Id == sessionId, cancellationToken);

    public async Task<IReadOnlyList<Session>> ListActiveForUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sessions = await context.Sessions
            .Where(s => s.UserId == userId && s.RevokedAtUtc == null && s.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
        return sessions;
    }

    public void Add(Session session) => context.Sessions.Add(session);
}
