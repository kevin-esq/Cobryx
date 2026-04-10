using Microsoft.Extensions.Logging;

namespace Cobryx.Application.Lending.Services;

public partial class LoanPaymentService
{
    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Payment {Reference} processed for Loan {LoanId}. Principal: {P}, Interest: {I}, Fees: {F}")]
    public static partial void LogPaymentProcessed(ILogger logger, string reference, Guid loanId, decimal p, decimal i, decimal f);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Error processing payment for Loan {LoanId}")]
    public static partial void LogPaymentError(ILogger logger, Exception ex, Guid loanId);
}
