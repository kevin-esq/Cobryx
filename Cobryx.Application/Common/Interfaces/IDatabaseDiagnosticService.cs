namespace Cobryx.Application.Common.Interfaces;

public interface IDatabaseDiagnosticService
{
    /// <summary>
    /// Gets the ratio of current XID age vs freeze_max_age. 
    /// 1.0 means critical risk of wraparound panic.
    /// </summary>
    Task<double> GetWraparoundRiskRatioAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the ratio of dead tuples vs live tuples for critical ledger tables.
    /// </summary>
    Task<double> GetLedgerDeadTupleRatioAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets the WAL sync duration (latency) in seconds.
    /// </summary>
    Task<double> GetWalSyncDurationAsync(CancellationToken ct = default);

    /// <summary>
    /// Collects and records all elite infrastructure metrics to CobryxMetrics.
    /// This should be called by a background job or periodic task.
    /// </summary>
    Task CollectInfrastructureMetricsAsync(CancellationToken ct = default);
}
