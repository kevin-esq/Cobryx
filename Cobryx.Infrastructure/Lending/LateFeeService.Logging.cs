using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public partial class LateFeeService
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Assessed Late Fee of {Amount} for Loan {LoanId} (DPD: {Dpd})")]
    public static partial void LogLateFeeAssessed(ILogger logger, decimal amount, Guid loanId, int dpd);
}
