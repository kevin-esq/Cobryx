using System;
using System.Collections.Generic;

namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Represents a single installment in a loan amortization schedule.
/// </summary>
/// <param name="Id">Internal identifier for the installment resource.</param>
/// <param name="Number">Sequential number of the installment (1-based).</param>
/// <param name="DueDate">Target payment date (ISO-8601).</param>
/// <param name="PrincipalAmount">Target principal payment (Decimal, 2-digit precision).</param>
/// <param name="InterestAmount">Target interest payment (Decimal, 2-digit precision).</param>
/// <param name="TotalAmount">Total amount due for this period (Principal + Interest).</param>
/// <param name="PrincipalPaid">Amount of principal successfully covered.</param>
/// <param name="InterestPaid">Amount of interest successfully covered.</param>
/// <param name="LateFeesPaid">Amount of late fees successfully covered.</param>
/// <param name="TotalPaid">Total amount successfully paid for this period.</param>
/// <param name="RemainingAmount">Outstanding balance for this specific installment.</param>
/// <param name="Status">Current status (e.g., Pending, Paid, Overdue, Partial).</param>
/// <param name="PaidAt">Actual date of full payment completion, if applicable.</param>
public record InstallmentContract(
    Guid Id,
    int Number,
    DateTime DueDate,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal TotalAmount,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal LateFeesPaid,
    decimal TotalPaid,
    decimal RemainingAmount,
    string Status,
    DateTime? PaidAt
);

/// <summary>
/// Professional amortization schedule providing a detailed breakdown of loan repayment.
/// </summary>
/// <param name="LoanId">Unique identifier for the parent loan.</param>
/// <param name="LoanNumber">User-friendly loan reference number.</param>
/// <param name="Status">Aggregated status of the loan lifecycle.</param>
/// <param name="OriginalPrincipal">The initial amount borrowed.</param>
/// <param name="TotalInterest">Aggregated total interest projected over the loan term.</param>
/// <param name="TotalPaid">Total amount paid to date across all installments.</param>
/// <param name="Installments">Detailed list of period-by-period obligations.</param>
public record AmortizationScheduleContract(
    Guid LoanId,
    string LoanNumber,
    string Status,
    decimal OriginalPrincipal,
    decimal TotalInterest,
    decimal TotalPaid,
    List<InstallmentContract> Installments
);
