using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Lending.Commands.RegisterPayment;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, Result<PaymentResultDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentApplicationPolicyRepository _policyRepository;
    private readonly IPaymentApplicationService _paymentAppService;
    private readonly ITenantProvider _tenantProvider;

    public RegisterPaymentHandler(
        ILoanRepository loanRepository,
        IPaymentRepository paymentRepository,
        IPaymentApplicationPolicyRepository policyRepository,
        IPaymentApplicationService paymentAppService,
        ITenantProvider tenantProvider)
    {
        _loanRepository = loanRepository;
        _paymentRepository = paymentRepository;
        _policyRepository = policyRepository;
        _paymentAppService = paymentAppService;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<PaymentResultDto>> Handle(RegisterPaymentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        // 1. Load Loan with installments
        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId, ct);
        if (loan == null || loan.TenantId != tenantId.Value)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        // 2. Load Policy (Default for tenant)
        var policy = await _policyRepository.GetDefaultByTenantAsync(tenantId.Value, ct);
        if (policy == null)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        // 3. Create Payment entity
        var payment = new Payment(
            tenantId.Value,
            loan.CustomerId,
            request.PaymentMethodId,
            new Money(request.Amount, "MXN"), // Currency could be dynamic
            request.PaidAt,
            request.Reference,
            request.Notes);

        // 4. Apply Payment via Domain Service
        var allocations = _paymentAppService.Apply(loan, request.Amount, policy);

        // 5. Update Payment aggregate with allocations (if the entity supports it, otherwise keep it simple for now)
        // Note: The Domain IPaymentApplicationService already updated the installments in the memory 'loan' object.
        
        // Record payment in loan
        loan.RecordPaymentApplied(request.Amount, request.PaidAt);
        loan.RecalculateBalances();
        loan.UpdateRiskStatus();

        // 6. Persist both
        await _paymentRepository.AddAsync(payment, ct);
        await _loanRepository.UpdateAsync(loan, ct);

        var excessCredit = allocations
            .Where(a => a.InstallmentId == null)
            .Sum(a => a.Amount);

        return Result.Success(new PaymentResultDto(
            payment.Id,
            request.Amount - excessCredit,
            loan.CurrentPrincipalBalance + loan.CurrentInterestBalance,
            excessCredit
        ));
    }
}
