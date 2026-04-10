using System.Text.Json;

using Cobryx.Application.Common.Interfaces;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.Services;

public class RedisCollectionsPriorityStore(IConnectionMultiplexer redis) : ICollectionsPriorityStore
{
    public async Task<List<PriorityCaseEntry>> GetTopPriorityCasesAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        IDatabase db = redis.GetDatabase();
        var priorityKey = $"portfolio:collections:priority:{tenantId}";

        SortedSetEntry[] topEntries = await db.SortedSetRangeByRankWithScoresAsync(
            priorityKey, 0, limit - 1, Order.Descending);

        var results = new List<PriorityCaseEntry>();

        foreach (SortedSetEntry entry in topEntries)
        {
            var loanId = entry.Element.ToString();
            var dataKey = $"portfolio:collections:data:{loanId}";

            RedisValue metadataJson = await db.HashGetAsync(dataKey, "info");

            if (!metadataJson.HasValue)
                continue;

            var metadata = JsonSerializer.Deserialize<object>(metadataJson!);
            results.Add(new PriorityCaseEntry(loanId!, entry.Score, metadata));
        }

        return results;
    }
}
