using System.Diagnostics;

using Cobryx.Infrastructure.Configuration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

using StackExchange.Redis;

namespace Cobryx.Infrastructure.HealthChecks;

public class RedisHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private const int DegradedThresholdMs = 100;
    private const int UnhealthyThresholdMs = 500;

    public RedisHealthCheck(IOptions<CachingOptions> options)
    {
        _connectionString = options.Value.Redis.ConnectionString;
    }

    public RedisHealthCheck(IConfiguration configuration)
    {
        _connectionString = configuration["Caching:Redis:ConnectionString"]
            ?? throw new InvalidOperationException("Redis ConnectionString is missing.");
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var sw = Stopwatch.StartNew();
            await using ConnectionMultiplexer connection = await ConnectionMultiplexer.ConnectAsync(_connectionString);
            IDatabase db = connection.GetDatabase();
            await db.PingAsync();
            sw.Stop();

            var rtt = sw.ElapsedMilliseconds;
            var data = new Dictionary<string, object>
            {
                { "RTT_ms", rtt },
                { "Instance", connection.Configuration }
            };

            if (rtt > UnhealthyThresholdMs)
            {
                return HealthCheckResult.Unhealthy($"Redis latency critical: {rtt}ms", data: data);
            }

            if (rtt > DegradedThresholdMs)
            {
                return HealthCheckResult.Degraded($"Redis latency high: {rtt}ms", data: data);
            }

            return HealthCheckResult.Healthy($"Redis RTT: {rtt}ms", data: data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy($"Redis health check failed: {ex.Message}");
        }
    }
}
