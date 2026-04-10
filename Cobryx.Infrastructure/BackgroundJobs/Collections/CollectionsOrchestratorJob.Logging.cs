using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.BackgroundJobs.Collections;

public partial class CollectionsOrchestratorJob
{
    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Starting Collections Orchestrator Job...")]
    public static partial void LogJobStarted(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Information,
        Message = "Found {count} delinquent loans.")]
    public static partial void LogDelinquentLoansFound(ILogger logger, int count);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Failed to process collection case for Loan {loanId}")]
    public static partial void LogProcessLoanFailed(ILogger logger, Exception? ex, Guid loanId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Information,
        Message = "Collections Orchestrator Job completed successfully.")]
    public static partial void LogJobCompleted(ILogger logger);
}
