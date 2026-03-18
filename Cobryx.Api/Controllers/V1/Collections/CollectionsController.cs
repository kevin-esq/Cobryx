using System.Text.Json;

using Cobryx.Application.Common.Interfaces;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using StackExchange.Redis;

namespace Cobryx.Api.Controllers.v1.Collections;

[ApiController]
[Route("api/v1/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ITenantProvider _tenantProvider;

    public CollectionsController(IConnectionMultiplexer redis, ITenantProvider tenantProvider)
    {
        _redis = redis;
        _tenantProvider = tenantProvider;
    }

    [HttpGet("cases")]
    public async Task<IActionResult> GetPriorityCases([FromQuery] int limit = 100)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (tenantId == null)
            return Unauthorized();

        var db = _redis.GetDatabase();
        var priorityKey = $"portfolio:collections:priority:{tenantId}";

        // Fetch top N loanIds by priority score (descending)
        var topEntries = await db.SortedSetRangeByRankWithScoresAsync(priorityKey, 0, limit - 1, Order.Descending);

        var results = new List<object>();

        foreach (var entry in topEntries)
        {
            var loanId = entry.Element.ToString();
            var dataKey = $"portfolio:collections:data:{loanId}";

            var metadataJson = await db.HashGetAsync(dataKey, "info");

            if (metadataJson.HasValue)
            {
                var metadata = JsonSerializer.Deserialize<object>(metadataJson!);
                results.Add(new
                {
                    priorityScore = entry.Score,
                    data = metadata
                });
            }
        }

        return Ok(new { cases = results });
    }
}
