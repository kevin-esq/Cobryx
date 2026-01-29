using Cobryx.Application.Common.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace Cobryx.Infrastructure.Security;

public class AuthAttemptService : IAuthAttemptService
{
    private readonly IDistributedCache _cache;
    private const string AttemptsPrefix = "auth_attempts_";
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

    public async Task IncrementAttemptsAsync(string ipAddress)
    {
        var attempts = await GetAttemptCountAsync(ipAddress) + 1;

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1)
        };

        await _cache.SetStringAsync(AttemptsPrefix + ipAddress, attempts.ToString(), options);
        await _cache.SetStringAsync(LastAttemptPrefix + ipAddress, DateTime.UtcNow.ToString("O"), options);
    }

    public async Task ResetAttemptsAsync(string ipAddress)
    {
        await _cache.RemoveAsync(AttemptsPrefix + ipAddress);
        await _cache.RemoveAsync(LastAttemptPrefix + ipAddress);
    }
}
