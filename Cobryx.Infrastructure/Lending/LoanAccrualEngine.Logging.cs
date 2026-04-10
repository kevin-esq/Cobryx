using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public partial class LoanAccrualEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Starting daily accrual for {AccrualDate}")]
    public static partial void LogStartingDailyAccrual(ILogger logger, DateTime accrualDate);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to process accrual for Loan {LoanId}")]
    public static partial void LogAccrualProcessingFailed(ILogger logger, Exception ex, Guid loanId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Finished daily accrual. Processed {LoanCount} loans, created {ChargeCount} charges.")]
    public static partial void LogDailyAccrualFinished(ILogger logger, int loanCount, int chargeCount);
}
