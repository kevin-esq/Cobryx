using System.Data.Common;

using Cobryx.Application.Common.Observability;

using Microsoft.EntityFrameworkCore.Diagnostics;

using Npgsql;

namespace Cobryx.Infrastructure.Persistence.Interceptors;

public class DbMetricsInterceptor : DbCommandInterceptor
{
    private readonly CobryxMetrics _metrics;

    public DbMetricsInterceptor(CobryxMetrics metrics)
    {
        _metrics = metrics;
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        _metrics.ConcurrentDbCommands.Add(1);
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        _metrics.ConcurrentDbCommands.Add(1);
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        _metrics.ConcurrentDbCommands.Add(-1);
        _metrics.DbCommandDuration.Record(eventData.Duration.TotalSeconds);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        _metrics.ConcurrentDbCommands.Add(-1);
        _metrics.DbCommandDuration.Record(eventData.Duration.TotalSeconds);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _metrics.ConcurrentDbCommands.Add(-1);
        RecordFailure(eventData.Exception);
        base.CommandFailed(command, eventData);
    }

    public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _metrics.ConcurrentDbCommands.Add(-1);
        RecordFailure(eventData.Exception);
        return base.CommandFailedAsync(command, eventData, cancellationToken);
    }

    private void RecordFailure(Exception ex)
    {
        if (ex is NpgsqlException nex)
        {
            if (nex.Message.Contains("Timeout while getting a connection from the pool", StringComparison.OrdinalIgnoreCase))
            {
                _metrics.DbPoolExhaustionTotal.Add(1);
            }

            if (nex.InnerException is System.TimeoutException || nex.IsTransient)
            {
                _metrics.DbRetryTotal.Add(1);
            }

            if (nex.Message.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            {
                _metrics.DbCommandTimeoutTotal.Add(1);
            }
        }
    }
}
