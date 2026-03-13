using System.Text.Json;

using Cobryx.Application.Common.Interfaces;

using Microsoft.Extensions.Caching.Distributed;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.Caching;

public class RedisCacheService : ICacheService
{
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly int _defaultTTLSeconds;

    public RedisCacheService(IDistributedCache cache, IConnectionMultiplexer redis, int defaultTTLSeconds = 300)
    {
        _cache = cache;
        _redis = redis;
        _defaultTTLSeconds = defaultTTLSeconds;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var data = await _cache.GetStringAsync(key, cancellationToken);

        if (string.IsNullOrEmpty(data))
            return default;

        return JsonSerializer.Deserialize<T>(data);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromSeconds(_defaultTTLSeconds)
        };

        var serialized = JsonSerializer.Serialize(value);
        await _cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(key, cancellationToken);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public async Task<bool> TryAtomicHashUpdateIfNewerAsync(
        string key,
        IDictionary<string, string> fields,
        long newSequence,
        string sequenceFieldName,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();

        // Lua script to perform atomic sequence-check and update
        // ARGV[1]: sequence field name
        // ARGV[2]: new sequence value
        // Remaining ARGV members: field-value pairs for the hash
        const string luaScript = @"
            local seq = redis.call('HGET', KEYS[1], ARGV[1])
            local newSeq = tonumber(ARGV[2])

            if not seq or newSeq > tonumber(seq) then
                for i = 3, #ARGV, 2 do
                    redis.call('HSET', KEYS[1], ARGV[i], ARGV[i+1])
                end
                return 1
            end
            return 0";

        var args = new List<RedisValue> { sequenceFieldName, newSequence };
        foreach (var field in fields)
        {
            args.Add(field.Key);
            args.Add(field.Value);
        }

        var result = await db.ScriptEvaluateAsync(luaScript, new RedisKey[] { key }, args.ToArray());
        var updated = (int)result == 1;

        if (updated)
        {
            await db.KeyExpireAsync(key, expiration ?? TimeSpan.FromSeconds(_defaultTTLSeconds));
        }

        return updated;
    }

    public async Task<IDictionary<string, string>?> GetHashAllAsync(string key, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var entries = await db.HashGetAllAsync(key);

        if (entries.Length == 0)
            return null;

        var result = new Dictionary<string, string>();
        foreach (var entry in entries)
        {
            result[entry.Name!] = entry.Value!;
        }

        return result;
    }
}
