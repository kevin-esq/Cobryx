using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
using Concordia;

namespace Cobryx.Application.Lending.Queries.GetLoan;

public record GetLoanQuery(Guid Id) : IRequest<Result<LoanDto>>;

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
