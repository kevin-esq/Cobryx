using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.Exceptions;
using Concordia;

namespace Cobryx.Application.Lending.Commands.CreateLoan;

/// <summary>
/// Handler for creating a new loan.
/// </summary>
public class CreateLoanHandler : IRequestHandler<CreateLoanCommand, Result<Guid>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly ILoanAgreementRepository _agreementRepository;
    private readonly IInterestPolicyRepository _interestPolicyRepository;
    private readonly IPaymentApplicationPolicyRepository _paymentPolicyRepository;
    private readonly IAmortizationService _amortizationService;
    private readonly ITenantProvider _tenantProvider;

    public CreateLoanHandler(
        ILoanRepository loanRepository,
        ILoanAgreementRepository agreementRepository,
        IInterestPolicyRepository interestPolicyRepository,
        IPaymentApplicationPolicyRepository paymentPolicyRepository,
        IAmortizationService amortizationService,
        ITenantProvider tenantProvider)
    {
        _loanRepository = loanRepository;
        _agreementRepository = agreementRepository;
        _interestPolicyRepository = interestPolicyRepository;
        _paymentPolicyRepository = paymentPolicyRepository;
        _amortizationService = amortizationService;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreateLoanCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId() ?? request.TenantId;
        if (tenantId == null || tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        // 1. Load Policies
        var interestPolicy = await _interestPolicyRepository.GetByIdAsync(request.InterestPolicyId, ct);
        if (interestPolicy == null)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        var paymentPolicy = await _paymentPolicyRepository.GetByIdAsync(request.PaymentApplicationPolicyId, ct);
        if (paymentPolicy == null)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        // 2. Create Agreement
        var agreement = new LoanAgreement(
            tenantId: tenantId.Value,
            customerId: request.CustomerId,
            principalAmount: request.PrincipalAmount,
            interestPolicyId: request.InterestPolicyId,
            paymentFrequency: request.PaymentFrequency,
            numberOfInstallments: request.NumberOfInstallments,
            startDate: DateTime.UtcNow,
            firstPaymentDate: request.FirstDueDate,
            origin: request.Origin,
            lateFeePolicyId: request.LateFeePolicyId);

        // 3. Generate Schedule
        var installments = _amortizationService.GenerateSchedule(agreement, interestPolicy);

        // 4. Create Loan
        var loanNumber = request.LoanNumber ?? $"LN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}";
        
        var loan = new Loan(
            tenantId.Value,
            request.CustomerId,
            agreement.Id,
            loanNumber,
            request.PrincipalAmount);

        // Link installments to loan
        foreach (var installment in installments)
        {
            installment.SetLoanId(loan.Id);
        }
        loan.AddInstallments(installments);

        // Sign agreement as it's now finalized
        agreement.Sign();

        // 5. Persist
        await _agreementRepository.AddAsync(agreement, ct);
        await _loanRepository.AddAsync(loan, ct);

        return Result.Success(loan.Id);
    }
}
