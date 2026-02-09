using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
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
    Guid InterestPolicyId,
    Guid? LateFeePolicyId,
    Guid PaymentApplicationPolicyId,
    LoanOrigin Origin,
    DateTime FirstDueDate,
    string? LoanNumber = null,
    Guid? TenantId = null
) : IRequest<Result<Guid>>;
