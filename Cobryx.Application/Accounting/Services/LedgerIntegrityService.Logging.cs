using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public partial class LedgerIntegrityService
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Resuming Incremental Integrity Scan from Checkpoint (SequenceId: {SequenceId}, Date: {Date:O})")]
    public static partial void LogResumingIncrementalScan(ILogger logger, long sequenceId, DateTime date);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting FORCED Full Ledger Integrity Scan for Tenant {TenantId}")]
    public static partial void LogStartingForcedFullScan(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Streaming Micro-Checkpoint: {Count} entries sealed (Tenant: {TenantId})")]
    public static partial void LogStreamingMicroCheckpoint(ILogger logger, int count, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Integrity Scan completed. Healthy: {IsHealthy}, Fingerprint: {Fingerprint}, Delta: {Delta}")]
    public static partial void LogIntegrityScanCompleted(ILogger logger, bool isHealthy, string fingerprint, int delta);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting Global Sequence Gap Detection (LAG-Optimized)")]
    public static partial void LogStartingGlobalGapDetection(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "SEQUENCE GAP DETECTED: Jump from {Prev} to {Curr}")]
    public static partial void LogSequenceGapDetected(ILogger logger, long prev, long curr);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting Global Ledger Sum Invariant Verification (Total Ledger Replay)")]
    public static partial void LogStartingGlobalSumVerification(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "GLOBAL INVARIANT FAILURE: Ledger Total Sum is {Balance}. Expected 0.")]
    public static partial void LogGlobalInvariantFailure(ILogger logger, decimal balance);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Verifying Snapshot {SnapshotId} for Account {AccountId} @ Seq {Seq}")]
    public static partial void LogVerifyingSnapshot(ILogger logger, Guid snapshotId, Guid accountId, long seq);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "SNAPSHOT CORRUPTION DETECTED: Account {AccountId}. Snapshot+Delta={Expected}, TotalReplay={Actual}")]
    public static partial void LogSnapshotCorruption(ILogger logger, Guid accountId, decimal expected, decimal actual);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Snapshot {SnapshotId} verified successfully against Total Ledger Replay.")]
    public static partial void LogSnapshotVerified(ILogger logger, Guid snapshotId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Attempting to trip circuit breaker for Tenant {TenantId}")]
    public static partial void LogAttemptingTripCircuitBreaker(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "DbContext is standard DbContext. Checking local cache.")]
    public static partial void LogDbContextStandard(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "DbContext is not standard DbContext. Using interface Tenants property.")]
    public static partial void LogDbContextNotStandard(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Critical,
        Message = "CRITICAL INTEGRITY FAILURE: Tripping Financial Circuit Breaker for Tenant {TenantId}")]
    public static partial void LogCriticalIntegrityFailure(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Metrics or CircuitBreakerTrippedTotal is null. Skipping metric recording.")]
    public static partial void LogMetricsNull(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not trip circuit breaker. Tenant found: {Found}")]
    public static partial void LogTripCircuitBreakerFailed(ILogger logger, bool found);
}
