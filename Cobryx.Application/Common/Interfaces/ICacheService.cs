namespace Cobryx.Application.Common.Interfaces;

public interface ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default);
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default);
    public Task RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically updates a Redis Hash if the provided sequence is greater than the existing one.
    /// Returns true if updated, false if rejected (stale event).
    /// </summary>
    public Task<bool> TryAtomicHashUpdateIfNewerAsync(
        string key,
        IDictionary<string, string> fields,
        long newSequence,
        string sequenceFieldName,
        TimeSpan? expiration = null,
        CancellationToken cancellationToken = default);

    public Task<IDictionary<string, string>?> GetHashAllAsync(string key, CancellationToken cancellationToken = default);
}
