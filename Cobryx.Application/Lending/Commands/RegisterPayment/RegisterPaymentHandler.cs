using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Entities.Lending;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Interfaces.Lending;
using Cobryx.Domain.Exceptions;
using Cobryx.Domain.ValueObjects;
using Concordia;
using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Lending.Commands.RegisterPayment;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, Result<PaymentResultDto>>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentApplicationPolicyRepository _policyRepository;
    private readonly IPaymentApplicationService _paymentAppService;
    private readonly ITenantProvider _tenantProvider;
    private readonly ISender _sender;
    private readonly IUnitOfWork _unitOfWork;

    public RegisterPaymentHandler(
        ILoanRepository loanRepository,
        IPaymentRepository paymentRepository,
        IPaymentApplicationPolicyRepository policyRepository,
        IPaymentApplicationService paymentAppService,
        ITenantProvider tenantProvider,
        ISender sender,
        IUnitOfWork unitOfWork)
    {
        _loanRepository = loanRepository;
        _paymentRepository = paymentRepository;
        _policyRepository = policyRepository;
        _paymentAppService = paymentAppService;
        _tenantProvider = tenantProvider;
        _sender = sender;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PaymentResultDto>> Handle(RegisterPaymentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId, ct);
        if (loan == null || loan.TenantId != tenantId.Value)
            throw new DomainException(DomainErrorCode.Loans.NotFound);
        var policy = await _policyRepository.GetDefaultByTenantAsync(tenantId.Value, ct);
        if (policy == null)
            throw new DomainException(DomainErrorCode.Loans.NotFound);

        var payment = new Payment(
            tenantId.Value,
            loan.CustomerId,
            request.PaymentMethodId,
            new Money(request.Amount, CobryxDefaults.Currency),
            request.PaidAt,
            request.Reference,
            request.Notes);

        var allocations = _paymentAppService.Apply(loan, request.Amount, policy);

        loan.RecordPaymentApplied(request.Amount, request.PaidAt);
        loan.RecalculateBalances();
        loan.UpdateFinancialRiskStatus(DateTime.UtcNow);
        await _paymentRepository.AddAsync(payment, ct);
        await _loanRepository.UpdateAsync(loan, ct);

        // Telemetry: Value Realization
        var db = (DbContext)_unitOfWork;
        var paymentsCount = await db.Set<Payment>()
            .CountAsync(p => p.TenantId == tenantId.Value && p.Status == Domain.Enums.PaymentStatus.Completed && !p.IsDemo, ct);

        if (paymentsCount == 1) // First real payment
        {
            var tenant = await db.Set<Tenant>().FirstAsync(t => t.Id == tenantId.Value, ct);
            tenant.TriggerOnboardingMilestone("REALIZING_VALUE");
        }

        await _unitOfWork.SaveChangesAsync(ct);

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
