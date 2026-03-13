using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.CloseLoan;

public class CloseLoanHandler : IRequestHandler<CloseLoanCommand, Result>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ITenantProvider _tenantProvider;

    public CloseLoanHandler(ILoanRepository loanRepository, ITenantProvider tenantProvider)
    {
        _loanRepository = loanRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(CloseLoanCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId, ct);
        if (loan == null || loan.TenantId != tenantId.Value)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        if (loan.CurrentPrincipalBalance > 0 || loan.CurrentInterestBalance > 0 || loan.CurrentLateFeeBalance > 0)
        {
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
        }
        if (loan.Installments.Any(i => i.Status != InstallmentStatus.Paid))
        {
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
        }

        loan.MarkAsClosed();

        await _loanRepository.UpdateAsync(loan, ct);

        return Result.Success();
    }
}
