using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Common.Observability;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Npgsql;

namespace Cobryx.Infrastructure.Persistence;

public class DatabaseDiagnosticService : IDatabaseDiagnosticService
{
    private readonly string _diagConnectionString;
    private readonly CobryxMetrics _metrics;
    private readonly ILogger<DatabaseDiagnosticService> _logger;

    private (double Value, DateTime Timestamp) _wraparoundRiskCache;
    private (double Value, DateTime Timestamp) _deadTupleRatioCache;
    private (double Value, DateTime Timestamp) _walSyncCache;

    private static readonly TimeSpan StructuralTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan DynamicTtl = TimeSpan.FromMinutes(2);

    public DatabaseDiagnosticService(
        IConfiguration configuration,
        CobryxMetrics metrics,
        ILogger<DatabaseDiagnosticService> logger)
    {
        _metrics = metrics;
        _logger = logger;

        var baseConnString = configuration.GetConnectionString("DefaultConnection");
        var builder = new NpgsqlConnectionStringBuilder(baseConnString)
        {
            ApplicationName = "CobryxDiag",
            MaxPoolSize = 2,
            Timeout = 5,
            CommandTimeout = 5
        };
        _diagConnectionString = builder.ToString();

        CobryxMetrics.RegisterInfrastructureProviders(
            () => _wraparoundRiskCache.Value,
            () => _walSyncCache.Value,
            () => _deadTupleRatioCache.Value,
            () => 0.0,
            () => 0.0
        );
    }

    public async Task<double> GetWraparoundRiskRatioAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _wraparoundRiskCache.Timestamp < StructuralTtl)
            return _wraparoundRiskCache.Value;

        try
        {
            using var conn = new NpgsqlConnection(_diagConnectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT MAX(age(relfrozenxid)::float / current_setting('autovacuum_freeze_max_age')::float)
                FROM pg_class
                WHERE relkind = 'r';";

            var result = await cmd.ExecuteScalarAsync(ct);
            var ratio = result is double d ? d : 0.0;

            _wraparoundRiskCache = (ratio, DateTime.UtcNow);
            return ratio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query XID Wraparound Risk Ratio");
            return _wraparoundRiskCache.Value;
        }
    }

    public async Task<double> GetLedgerDeadTupleRatioAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _deadTupleRatioCache.Timestamp < DynamicTtl)
            return _deadTupleRatioCache.Value;

        try
        {
            using var conn = new NpgsqlConnection(_diagConnectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT SUM(n_dead_tup)::float / NULLIF(SUM(n_live_tup + n_dead_tup), 0)::float
                FROM pg_stat_user_tables
                WHERE schemaname = 'public' 
                  AND relname IN ('LedgerEntries', 'LedgerTransactions', 'BankMovements');";

            var result = await cmd.ExecuteScalarAsync(ct);
            var ratio = result != DBNull.Value ? Convert.ToDouble(result) : 0.0;

            _deadTupleRatioCache = (ratio, DateTime.UtcNow);
            return ratio;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Ledger Dead Tuple Ratio");
            return _deadTupleRatioCache.Value;
        }
    }

    public async Task<double> GetWalSyncDurationAsync(CancellationToken ct = default)
    {
        if (DateTime.UtcNow - _walSyncCache.Timestamp < DynamicTtl)
            return _walSyncCache.Value;

        try
        {
            using var conn = new NpgsqlConnection(_diagConnectionString);
            await conn.OpenAsync(ct);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT (sync_time / NULLIF(syncs, 0)) / 1000.0 FROM pg_stat_wal;";

            var result = await cmd.ExecuteScalarAsync(ct);
            var latencySeconds = result != DBNull.Value ? Convert.ToDouble(result) : 0.0;

            _walSyncCache = (latencySeconds, DateTime.UtcNow);
            return latencySeconds;
        }
        catch (Exception ex)
        {
            _logger.LogWarning("pg_stat_wal query failed (might be an older PG version): {Message}", ex.Message);
            return 0.0;
        }
    }

    public async Task CollectInfrastructureMetricsAsync(CancellationToken ct = default)
    {
        _logger.LogDebug("Refreshing elite infrastructure diagnostics...");

        var xidRisk = await GetWraparoundRiskRatioAsync(ct);
        await GetLedgerDeadTupleRatioAsync(ct);
        await GetWalSyncDurationAsync(ct);

        if (xidRisk > 0.85)
        {
            _logger.LogCritical("EMERGENCY DB PRESSURE: Postgres Wraparound Risk Ratio is at {Ratio:P2}!", xidRisk);
        }
    }
}
