using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Accounting.Services;

public partial class FinancialPostingEngine
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Posting payment allocation for Loan {LoanId}, Payment {PaymentId}")]
    public static partial void LogPostingPaymentAllocation(ILogger logger, Guid loanId, Guid paymentId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Posting payment of {Amount} for Loan {LoanId}")]
    public static partial void LogPostingPayment(ILogger logger, decimal amount, Guid loanId);
}
