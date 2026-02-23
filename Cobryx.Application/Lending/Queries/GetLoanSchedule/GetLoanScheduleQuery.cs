using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.Lending.Dtos;
using Cobryx.Domain.Common;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.Exceptions;
using Concordia;

namespace Cobryx.Application.Lending.Queries.GetLoanSchedule;

/// <summary>
/// Query to retrieve the full amortization schedule and status of a loan.
/// </summary>
public record GetLoanScheduleQuery(Guid LoanId) : IRequest<Result<LoanScheduleDto>>;

public class GetLoanScheduleHandler : IRequestHandler<GetLoanScheduleQuery, Result<LoanScheduleDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetLoanScheduleHandler(ILoanRepository loanRepository, ITenantProvider tenantProvider)
    {
        _loanRepository = loanRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<LoanScheduleDto>> Handle(GetLoanScheduleQuery request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId, ct);

        if (loan == null || loan.TenantId != tenantId.Value)
        {
            throw new DomainException(DomainErrorCode.Loans.NotFound);
        }

        var installmentsDto = loan.Installments
            .OrderBy(i => i.InstallmentNumber)
            .Select(i => new InstallmentDto(
                i.Id,
                i.InstallmentNumber,
                i.DueDate,
                i.PrincipalAmount,
                i.InterestAmount,
                i.TotalAmount,
                i.PrincipalPaid,
                i.InterestPaid,
                i.LateFeePaid,
                i.TotalPaid,
                i.RemainingAmount,
                i.Status,
                i.PaidAt))
            .ToList();

        return Result.Success(new LoanScheduleDto(
            loan.Id,
            loan.LoanNumber,
            loan.Status,
            loan.OriginalPrincipal,
            loan.Installments.Sum(i => i.InterestAmount),
            loan.TotalPaid,
            installmentsDto
        ));
    }
}
