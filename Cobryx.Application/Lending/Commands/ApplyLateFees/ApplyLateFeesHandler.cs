using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.ApplyLateFees;

public class ApplyLateFeesHandler(
    ILoanRepository loanRepository,
    ILateFeePolicyRepository policyRepository,
    ILoanAgreementRepository agreementRepository,
    ITenantProvider tenantProvider) : IRequestHandler<ApplyLateFeesCommand, Result<LateFeeResultDto>>
{
    private readonly ILoanRepository _loanRepository = loanRepository;
    private readonly ILateFeePolicyRepository _policyRepository = policyRepository;
    private readonly ILoanAgreementRepository _agreementRepository = agreementRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    public async Task<Result<LateFeeResultDto>> Handle(ApplyLateFeesCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var loansToProcess = new List<Loan>();

        if (request.LoanId.HasValue)
        {
            var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId.Value, ct);
            if (loan != null && loan.TenantId == tenantId.Value)
                loansToProcess.Add(loan);
        }
        else
        {
            var activeLoans = await _loanRepository.GetActiveByTenantAsync(tenantId.Value, ct);
            foreach (var activeLoan in activeLoans)
            {
                var fullLoan = await _loanRepository.GetByIdWithInstallmentsAsync(activeLoan.Id, ct);
                if (fullLoan != null) loansToProcess.Add(fullLoan);
            }
        }

        int affectedLoans = 0;
        int affectedInstallments = 0;
        decimal totalFees = 0;

        foreach (var loan in loansToProcess)
        {
            var agreement = await _agreementRepository.GetByIdAsync(loan.LoanAgreementId, ct);
            if (agreement == null || !agreement.LateFeePolicyId.HasValue) continue;

            var policy = await _policyRepository.GetByIdAsync(agreement.LateFeePolicyId.Value, ct);
            if (policy == null) continue;

            bool loanUpdated = false;
            var today = DateTime.UtcNow.Date;

            foreach (var installment in loan.Installments.Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < today))
            {
                var daysLate = (int)(today - installment.DueDate).TotalDays;
                var fee = policy.CalculateLateFee(daysLate, installment.PrincipalAmount + installment.InterestAmount - installment.PrincipalPaid.Amount - installment.InterestPaid.Amount);

                if (fee > 0)
                {
                    loan.AssessLateFees(fee);
                    totalFees += fee;
                    affectedInstallments++;
                    loanUpdated = true;
                }
            }

            if (loanUpdated)
            {
                loan.UpdateFinancialRiskStatus();
                await _loanRepository.UpdateAsync(loan, ct);
                affectedLoans++;
            }
        }

        return Result.Success(new LateFeeResultDto(affectedLoans, affectedInstallments, totalFees));
    }
}
