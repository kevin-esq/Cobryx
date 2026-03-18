using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Lending.Commands.CreateLoan;

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
    IUnitOfWork unitOfWork) : IRequestHandler<CreateLoanCommand, Result<Guid>>
{
    private readonly ILoanRepository _loanRepository = loanRepository;
    private readonly ILoanAgreementRepository _agreementRepository = agreementRepository;
    private readonly IInterestPolicyRepository _interestPolicyRepository = interestPolicyRepository;
    private readonly IPaymentApplicationPolicyRepository _paymentPolicyRepository = paymentPolicyRepository;
    private readonly IAmortizationService _amortizationService = amortizationService;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ISender _sender = sender;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<Guid>> Handle(CreateLoanCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == null || tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var interestPolicy = await _interestPolicyRepository.GetByCodeAsync(tenantId.Value, request.InterestPolicyCode, ct) ?? throw new DomainException(DomainErrorCode.Loans.NotFound);

        var paymentPolicy = await _paymentPolicyRepository.GetByCodeAsync(tenantId.Value, request.PaymentApplicationPolicyCode, ct) ?? throw new DomainException(DomainErrorCode.Loans.NotFound);

        InterestPolicy? lateFeePolicy = null;
        if (!string.IsNullOrWhiteSpace(request.LateFeePolicyCode))
        {
            lateFeePolicy = await _interestPolicyRepository.GetByCodeAsync(tenantId.Value, request.LateFeePolicyCode, ct);
        }

        var agreement = new LoanAgreement(
            tenantId: tenantId.Value,
            customerId: request.CustomerId,
            principalAmount: request.PrincipalAmount,
            interestPolicyId: interestPolicy.Id,
            paymentFrequency: request.PaymentFrequency,
            numberOfInstallments: request.NumberOfInstallments,
            startDate: DateTime.UtcNow,
            firstPaymentDate: request.FirstDueDate,
            origin: request.Origin,
            lateFeePolicyId: lateFeePolicy?.Id);

        var installments = _amortizationService.GenerateSchedule(agreement, interestPolicy);

        var loanNumber = request.LoanNumber ?? $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var loan = new Loan(
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

        foreach (var installment in installments)
        {
            installment.SetLoanId(loan.Id);
        }
        loan.AddInstallments(installments);

        agreement.Sign();

        var dbContext = (DbContext)_unitOfWork;

        var existingCount = await dbContext.Set<Loan>()
            .CountAsync(l => l.TenantId == tenantId.Value && !l.IsDemo, ct);

        var isFirstRealLoan = existingCount == 0;

        await _agreementRepository.AddAsync(agreement, ct);
        await _loanRepository.AddAsync(loan, ct);

        if (isFirstRealLoan)
        {
            await _sender.Send(new Tenants.Commands.PurgeDemoData.PurgeDemoDataCommand(), ct);

            var tenant = await dbContext.Set<Tenant>()
                .FirstAsync(t => t.Id == tenantId.Value, ct);
            tenant.TriggerOnboardingMilestone("CREATING_ASSETS");
        }

        await dbContext.SaveChangesAsync(ct);

        return Result.Success(loan.Id);
    }
}
