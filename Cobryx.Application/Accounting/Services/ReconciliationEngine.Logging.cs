using Cobryx.Domain.Accounting.Enums;

using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public partial class ReconciliationEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting reconciliation run {RunId} for tenant {TenantId}")]
    public static partial void LogReconciliationStarted(ILogger logger, Guid runId, Guid tenantId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Reconciliation run {RunId} completed with status {Status}")]
    public static partial void LogReconciliationCompleted(ILogger logger, Guid runId, ReconciliationStatus status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Drift {Id} auto-repaired after confirmation.")]
    public static partial void LogDriftAutoRepaired(ILogger logger, string id);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto-repair: re-posting missing payment {IntentId} for loan {LoanId}")]
    public static partial void LogAutoRepairPosting(ILogger logger, string intentId, Guid loanId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Auto-repair failed for intent {IntentId}")]
    public static partial void LogAutoRepairFailed(ILogger logger, Exception ex, string intentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Auto-repair SUCCESS: Payment {IntentId} for loan {LoanId} committed atomically")]
    public static partial void LogAutoRepairSuccess(ILogger logger, string intentId, Guid loanId);
}
