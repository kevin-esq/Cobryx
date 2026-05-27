using Cobryx.Domain.Lending.Enums;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;
using Cobryx.Application.Common.Attributes;

namespace Cobryx.Application.Lending.Queries.GetLoan;

[TenantScoped]
public record GetLoanQuery(Guid Id) : IRequest<Result<LoanDto>>, IRequiresTenant;

public record LoanDto(
    Guid Id,
    string LoanNumber,
    Guid CustomerId,
    decimal PrincipalAmount,
    decimal InterestRate,
    int InstallmentsCount,
    string Currency,
    LoanStatus Status,
    DateTime CreatedAt,
    decimal RemainingBalance);
