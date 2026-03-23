using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Identity;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Lending;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Payments.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

using Microsoft.EntityFrameworkCore;

namespace Cobryx.Application.Lending.Commands.RegisterPayment;

public class RegisterPaymentHandler(
    ILoanRepository loanRepository,
    IPaymentRepository paymentRepository,
    IPaymentApplicationPolicyRepository policyRepository,
    IPaymentApplicationService paymentAppService,
    ITenantProvider tenantProvider,
    ISender sender,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterPaymentCommand, Result<PaymentResultDto>>
{
    private readonly ILoanRepository _loanRepository = loanRepository;
    private readonly IPaymentRepository _paymentRepository = paymentRepository;
    private readonly IPaymentApplicationPolicyRepository _policyRepository = policyRepository;
    private readonly IPaymentApplicationService _paymentAppService = paymentAppService;
    private readonly ITenantProvider _tenantProvider = tenantProvider;
    private readonly ISender _sender = sender;
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<Result<PaymentResultDto>> Handle(RegisterPaymentCommand request, CancellationToken ct)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);

        var loan = await _loanRepository.GetByIdWithInstallmentsAsync(request.LoanId, ct);
        if (loan == null || loan.TenantId != tenantId.Value)
            throw new DomainException(DomainErrorCode.Loans.NotFound);
        var policy = await _policyRepository.GetDefaultByTenantAsync(tenantId.Value, ct) ?? throw new DomainException(DomainErrorCode.Loans.NotFound);

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
        loan.UpdateFinancialRiskStatus();
        await _paymentRepository.AddAsync(payment, ct);
        await _loanRepository.UpdateAsync(loan, ct);

        var db = (DbContext)_unitOfWork;
        var paymentsCount = await db.Set<Payment>()
            .CountAsync(p => p.TenantId == tenantId.Value && p.Status == PaymentStatus.Completed && !p.IsDemo, ct);

        if (paymentsCount == 1)
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
