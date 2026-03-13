using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Infrastructure.Repositories;

public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly CobryxDbContext _context;
    private readonly DbSet<RefreshToken> _dbSet;

    public RefreshTokenRepository(CobryxDbContext context)
    {
        _context = context;
        _dbSet = context.Set<RefreshToken>();
    }

    public void Add(RefreshToken token)
    {
        _dbSet.Add(token);
    }

    public async Task<RefreshToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(t => t.Token == tokenValue, cancellationToken);
    }

    public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(t => t.SessionId == sessionId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(t => t.UserId == userId && t.Revoked == null && t.Expires > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke("System", reason);
        }
    }

    public async Task RevokeAllForSessionAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var tokens = await _dbSet
            .Where(t => t.SessionId == sessionId && t.Revoked == null && t.Expires > DateTime.UtcNow)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.Revoke("System", reason);
        }
    }

    public async Task RemoveOldTokensAsync(int ttlDays, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-ttlDays);
        var oldTokens = await _dbSet
            .Where(t => !t.IsActive && t.CreatedAt <= cutoffDate)
            .ToListAsync(cancellationToken);

        _dbSet.RemoveRange(oldTokens);
    }
}
