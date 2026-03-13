using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Payments.Commands.Register;

public class RegisterPaymentHandler : IRequestHandler<RegisterPaymentCommand, Result<Guid>>
{
    private readonly ICreditRepository _creditRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantProvider _tenantProvider;

    public RegisterPaymentHandler(
        ICreditRepository creditRepository,
        IPaymentRepository paymentRepository,
        ITenantProvider tenantProvider)
    {
        _creditRepository = creditRepository;
        _paymentRepository = paymentRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
        {
            return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);
        }

        var credit = await _creditRepository.GetByIdAsync(request.CreditId, cancellationToken);
        if (credit == null || credit.TenantId != tenantId.Value)
        {
            return Result.Failure<Guid>(DomainErrorCode.Credits.NotFound);
        }

        var amount = new Money(request.Amount, request.Currency);

        var payment = new Payment(
            tenantId.Value,
            credit.CustomerId,
            request.PaymentMethodId,
            amount,
            request.PaymentDate,
            request.Reference,
            request.Notes);


        credit.ApplyPayment(payment.Id, amount);

        await _paymentRepository.AddAsync(payment, cancellationToken);
        await _creditRepository.UpdateAsync(credit, cancellationToken);

        return Result.Success(payment.Id);
    }
}
