using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public partial class BankReconciliationEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting Institutional Bank Reconciliation for Tenant {TenantId}")]
    public static partial void LogStartingReconciliation(ILogger logger, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Ambiguous Bank Movement {MovementId}: Found {Count} potential Strong matches. Jumping to Investigation.")]
    public static partial void LogAmbiguousMovement(ILogger logger, Guid movementId, int count);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Bank Reconciliation completed and Sealed. Fingerprint: {Fingerprint}")]
    public static partial void LogReconciliationCompleted(ILogger logger, string? fingerprint);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "System under Pressure (XID: {Risk:P}, DeadTuples: {Tuples:P}). Applying {Jitter}ms backoff.")]
    public static partial void LogSystemPressureBackoff(ILogger logger, double risk, double tuples, int jitter);
}
