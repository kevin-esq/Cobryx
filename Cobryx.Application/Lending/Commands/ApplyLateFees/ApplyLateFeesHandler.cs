using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Lending.Commands.ApplyLateFees
{
    public class ApplyLateFeesHandler(
        ILoanRepository loanRepository,
        ILateFeePolicyRepository policyRepository,
        ILoanAgreementRepository agreementRepository,
        ITenantProvider tenantProvider,
        IClock clock) : IRequestHandler<ApplyLateFeesCommand, Result<LateFeeResultDto>>
    {
        public async Task<Result<LateFeeResultDto>> Handle(ApplyLateFeesCommand request, CancellationToken cancellationToken)
        {
            Guid? tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<LateFeeResultDto>(DomainErrorCode.Tenant.ContextMissing);
            }

            List<Loan> loansToProcess = new();

            if (request.LoanId.HasValue)
            {
                Loan? loan = await loanRepository.GetByIdWithInstallmentsAsync(request.LoanId.Value, cancellationToken);
                if (loan != null && loan.TenantId == tenantId.Value)
                {
                    loansToProcess.Add(loan);
                }
            }
            else
            {
                IEnumerable<Loan> activeLoans = await loanRepository.GetActiveByTenantAsync(tenantId.Value, cancellationToken);
                foreach (Loan activeLoan in activeLoans)
                {
                    Loan? fullLoan = await loanRepository.GetByIdWithInstallmentsAsync(activeLoan.Id, cancellationToken);
                    if (fullLoan != null)
                    {
                        loansToProcess.Add(fullLoan);
                    }
                }
            }

            var affectedLoans = 0;
            var affectedInstallments = 0;
            decimal totalFees = 0;

            foreach (Loan loan in loansToProcess)
            {
                LoanAgreement? agreement = await agreementRepository.GetByIdAsync(loan.LoanAgreementId, cancellationToken);
                if (agreement == null || !agreement.LateFeePolicyId.HasValue)
                {
                    continue;
                }

                LateFeePolicy? policy = await policyRepository.GetByIdAsync(agreement.LateFeePolicyId.Value, cancellationToken);
                if (policy == null)
                {
                    continue;
                }

                var loanUpdated = false;
                DateTime today = clock.UtcNow.Date;

                foreach (Installment installment in loan.Installments.Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < today))
                {
                    var daysLate = (int)(today - installment.DueDate).TotalDays;
                    var fee = policy.CalculateLateFee(daysLate, installment.PrincipalAmount + installment.InterestAmount - installment.PrincipalPaid.Amount - installment.InterestPaid.Amount);

                    if (fee <= 0)
                        continue;
                    loan.AssessLateFees(fee);
                    totalFees += fee;
                    affectedInstallments++;
                    loanUpdated = true;
                }

                if (loanUpdated)
                {
                    loan.UpdateFinancialRiskStatus();
                    await loanRepository.UpdateAsync(loan, cancellationToken);
                    affectedLoans++;
                }
            }

            return Result.Success(new LateFeeResultDto(affectedLoans, affectedInstallments, totalFees));
        }
    }
}
