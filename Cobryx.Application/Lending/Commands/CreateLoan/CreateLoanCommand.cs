using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.CreateLoan;

/// <summary>
/// Command to create a new loan with its amortization schedule.
/// </summary>
public record CreateLoanCommand(
    Guid CustomerId,
    decimal PrincipalAmount,
    PaymentFrequency PaymentFrequency,
    int NumberOfInstallments,
    string InterestPolicyCode,
    string? LateFeePolicyCode,
    string PaymentApplicationPolicyCode,
    LoanOrigin Origin,
    DateTime FirstDueDate,
    string? LoanNumber = null,
    Guid? TenantId = null
) : IRequest<Result<Guid>>;
