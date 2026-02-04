using Cobryx.Domain.Entities;

namespace Cobryx.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    void Add(RefreshToken token);
    Task<RefreshToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<RefreshToken>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default);
    Task RevokeAllForSessionAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);
    Task RemoveOldTokensAsync(int ttlDays, CancellationToken cancellationToken = default);
}
