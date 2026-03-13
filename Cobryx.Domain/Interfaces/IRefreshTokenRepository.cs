using Cobryx.Domain.Identity;

namespace Cobryx.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    public void Add(RefreshToken token);
    public Task<RefreshToken?> GetByTokenValueAsync(string tokenValue, CancellationToken cancellationToken = default);
    public Task<List<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    public Task<List<RefreshToken>> GetBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken = default);
    public Task RevokeAllForUserAsync(Guid userId, string? reason = null, CancellationToken cancellationToken = default);
    public Task RevokeAllForSessionAsync(Guid sessionId, string? reason = null, CancellationToken cancellationToken = default);
    public Task RemoveOldTokensAsync(int ttlDays, CancellationToken cancellationToken = default);
}
