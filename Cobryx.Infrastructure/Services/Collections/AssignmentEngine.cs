using Cobryx.Application.Collections.Assignment;
using Cobryx.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Cobryx.Infrastructure.Services.Collections;

public class AssignmentEngine(IConnectionMultiplexer redis, ICobryxDbContext dbContext, ILogger<AssignmentEngine> logger) : IAssignmentEngine
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly ICobryxDbContext _dbContext = dbContext;
    private readonly ILogger<AssignmentEngine> _logger = logger;

    public async Task AssignCasesAsync(Guid tenantId)
    {
        var db = _redis.GetDatabase();
        var lockKey = $"portfolio:collections:assignment:lock:{tenantId}";
        var token = Guid.NewGuid().ToString();

        // 1. Acquire Distributed Lock
        var acquired = await db.LockTakeAsync(lockKey, token, TimeSpan.FromSeconds(30));
        if (!acquired)
        {
            _logger.LogInformation($"Assignment engine for tenant {tenantId} is already running elsewhere.");
            return;
        }

        try
        {
            // 2. Fetch Unassigned critical cases
            var unassignedCases = await _dbContext.CollectionCases
                .Where(c => c.TenantId == tenantId && !c.IsClosed && c.AssignedAgentId == null && c.PriorityScore > 0)
                .OrderByDescending(c => c.PriorityScore)
                .Take(100)
                .ToListAsync();

            if (!unassignedCases.Any()) return;

            // 3. Find Available Agents
            var activeAgents = await _dbContext.CollectionAgents
                .Where(a => a.TenantId == tenantId && a.IsActive && a.CurrentLoad < a.MaxCapacity)
                .ToListAsync();

            if (!activeAgents.Any()) return;

            // 4. Distribute using round-robin logic
            int agentIndex = 0;
            foreach (var caseToAssign in unassignedCases)
            {
                var agent = activeAgents[agentIndex];
                if (agent.CurrentLoad >= agent.MaxCapacity) continue;

                caseToAssign.AssignAgent(agent.Id);
                agent.CurrentLoad++;
                
                // Track load in Redis too for realtime dashboards
                await db.HashIncrementAsync($"portfolio:collections:agents:load:{tenantId}", agent.Id.ToString(), 1);

                agentIndex = (agentIndex + 1) % activeAgents.Count;
            }

            await _dbContext.SaveChangesAsync(default);
            _logger.LogInformation($"Successfully assigned cases using Redis Lock for tenant {tenantId}.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error during case assignment for tenant {tenantId}.");
        }
        finally
        {
            // 5. Release Lock
            await db.LockReleaseAsync(lockKey, token);
        }
    }
}
