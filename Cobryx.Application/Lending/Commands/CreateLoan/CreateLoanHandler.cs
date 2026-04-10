using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Lending.Commands.CreateLoan
{
    /// <summary>
    /// Handler for creating a new loan.
    /// </summary>
    public class CreateLoanHandler(
        ILoanRepository loanRepository,
        ILoanAgreementRepository agreementRepository,
        IInterestPolicyRepository interestPolicyRepository,
        IPaymentApplicationPolicyRepository paymentPolicyRepository,
        IAmortizationService amortizationService,
        ITenantProvider tenantProvider,
        ISender sender,
        IClock clock,
        IUnitOfWork unitOfWork) : IRequestHandler<CreateLoanCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
        {
            var tenantId = tenantProvider.GetTenantId();
            if (!tenantId.HasValue)
            {
                return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
            }

            InterestPolicy interestPolicy = await interestPolicyRepository.GetByCodeAsync(tenantId.Value, request.InterestPolicyCode, cancellationToken) ?? throw new DomainException(DomainErrorCode.Loans.NotFound);

            _ = await paymentPolicyRepository.GetByCodeAsync(tenantId.Value, request.PaymentApplicationPolicyCode, cancellationToken) ?? throw new DomainException(DomainErrorCode.Loans.NotFound);

            InterestPolicy? lateFeePolicy = null;
            if (!string.IsNullOrWhiteSpace(request.LateFeePolicyCode))
            {
                lateFeePolicy = await interestPolicyRepository.GetByCodeAsync(tenantId.Value, request.LateFeePolicyCode, cancellationToken);
            }

            LoanAgreement agreement = new(
                tenantId: tenantId.Value,
                customerId: request.CustomerId,
                principalAmount: request.PrincipalAmount,
                interestPolicyId: interestPolicy.Id,
                paymentFrequency: request.PaymentFrequency,
                numberOfInstallments: request.NumberOfInstallments,
                startDate: clock.UtcNow,
                firstPaymentDate: request.FirstDueDate,
                origin: request.Origin,
                lateFeePolicyId: lateFeePolicy?.Id);

            IReadOnlyList<Installment> installments = amortizationService.GenerateSchedule(agreement, interestPolicy);

            string loanNumber = request.LoanNumber ?? $"LN-{clock.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper(System.Globalization.CultureInfo.CurrentCulture)}";

            Loan loan = new(
                tenantId.Value,
                request.CustomerId,
                agreement.Id,
                loanNumber,
                new Money(request.PrincipalAmount, agreement.Currency),
                interestRate: interestPolicy.Rate ?? 0,
                interestType: (InterestType)interestPolicy.Method,
                frequency: agreement.PaymentFrequency,
                installmentsCount: agreement.NumberOfInstallments,
                graceDays: agreement.GracePeriodDays);

            foreach (Installment installment in installments)
            {
                installment.SetLoanId(loan.Id);
            }
            loan.AddInstallments(installments);

            agreement.Sign();

            DbContext dbContext = (DbContext)unitOfWork;

            int existingCount = await dbContext.Set<Loan>()
                .CountAsync(l => l.TenantId == tenantId.Value && !l.IsDemo, cancellationToken);

            bool isFirstRealLoan = existingCount == 0;

            await agreementRepository.AddAsync(agreement, cancellationToken);
            await loanRepository.AddAsync(loan, cancellationToken);

            if (isFirstRealLoan)
            {
                _ = await sender.Send(new Tenants.Commands.PurgeDemoData.PurgeDemoDataCommand(), cancellationToken);

                Tenant tenant = await dbContext.Set<Tenant>()
                    .FirstAsync(t => t.Id == tenantId.Value, cancellationToken);
                tenant.TriggerOnboardingMilestone("CREATING_ASSETS");
            }

            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success(loan.Id);
        }
    }
}
