using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.Exceptions;
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

        // Verify balance is truly zero
        if (loan.CurrentPrincipalBalance > 0 || loan.CurrentInterestBalance > 0 || loan.CurrentLateFeeBalance > 0)
        {
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
        }

        // Ensure all installments are marked as Paid
        if (loan.Installments.Any(i => i.Status != InstallmentStatus.Paid))
        {
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);
        }

        loan.MarkAsClosed();

        await _loanRepository.UpdateAsync(loan, ct);

        return Result.Success();
    }
}
