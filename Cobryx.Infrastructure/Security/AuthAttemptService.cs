using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Caching.Distributed;

namespace Cobryx.Infrastructure.Security;

public class AuthAttemptService : IAuthAttemptService
{
    private readonly IDistributedCache _cache;
    private const string AttemptsPrefix = "auth_attempts_ip_";
    private const string UserAttemptsPrefix = "auth_attempts_user_";
    private const string LastAttemptPrefix = "last_auth_attempt_";

    public AuthAttemptService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<int> GetAttemptCountAsync(string ipAddress)
    {
        var attemptsStr = await _cache.GetStringAsync(AttemptsPrefix + ipAddress);
        return int.TryParse(attemptsStr, out int attempts) ? attempts : 0;
    }

    public async Task<int> GetUserAttemptCountAsync(string email)
    {
        var attemptsStr = await _cache.GetStringAsync(UserAttemptsPrefix + email.ToLowerInvariant());
        return int.TryParse(attemptsStr, out int attempts) ? attempts : 0;
    }

    public async Task IncrementAttemptsAsync(string ipAddress, string? email = null)
    {
        var ipAttempts = await GetAttemptCountAsync(ipAddress) + 1;
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await _cache.SetStringAsync(AttemptsPrefix + ipAddress, ipAttempts.ToString(), options);
        await _cache.SetStringAsync(LastAttemptPrefix + ipAddress, DateTime.UtcNow.ToString("O"), options);

        if (!string.IsNullOrEmpty(email))
        {
            var userAttempts = await GetUserAttemptCountAsync(email) + 1;
            await _cache.SetStringAsync(UserAttemptsPrefix + email.ToLowerInvariant(), userAttempts.ToString(), options);
        }
    }

    public async Task ResetAttemptsAsync(string ipAddress, string? email = null)
    {
        await _cache.RemoveAsync(AttemptsPrefix + ipAddress);
        await _cache.RemoveAsync(LastAttemptPrefix + ipAddress);

        if (!string.IsNullOrEmpty(email))
        {
            await _cache.RemoveAsync(UserAttemptsPrefix + email.ToLowerInvariant());
        }
    }
}
