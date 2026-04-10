using Microsoft.Extensions.Logging;

namespace Cobryx.Infrastructure.Lending;

public partial class PaymentAllocationEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Allocating payment of {Amount} for Loan {LoanId}")]
    public static partial void LogAllocatingPayment(ILogger logger, decimal amount, Guid loanId);
}
