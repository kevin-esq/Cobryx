using Cobryx.Domain.Lending.Enums;

using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public partial class CollectionsEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting Daily Collections Evaluation for {Today}")]
    public static partial void LogStartingDailyEvaluation(ILogger logger, DateTime today);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Daily Collections Evaluation Completed.")]
    public static partial void LogDailyEvaluationCompleted(ILogger logger);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loan {LoanId} already evaluated for {Today}. Skipping.")]
    public static partial void LogLoanAlreadyEvaluated(ILogger logger, Guid loanId, DateTime today);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Loan {LoanId} reached Write-Off threshold ({Dpd} DPD). Automated Charge-Off posted.")]
    public static partial void LogAutoWriteOffPosted(ILogger logger, Guid loanId, int dpd);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Loan {LoanId} Stage Change: {Old} -> {New}")]
    public static partial void LogLoanStageChanged(ILogger logger, Guid loanId, DelinquencyStage old, DelinquencyStage @new);
}
