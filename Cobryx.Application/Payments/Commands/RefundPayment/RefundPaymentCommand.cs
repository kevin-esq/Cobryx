using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

using Concordia;

namespace Cobryx.Application.Payments.Commands.RefundPayment;

[TenantScoped]
public record RefundPaymentCommand(
    Guid PaymentId,
    decimal Amount,
    string Currency) : IRequest<Result>, IRequiresTenant;

public class RefundPaymentHandler(
    IPaymentRepository paymentRepository,
    ITenantProvider tenantProvider,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RefundPaymentCommand, Result>
{
    public async Task<Result> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var payment = await paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        if (payment.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var refundAmount = new Money(request.Amount, request.Currency);

        try
        {
            payment.Refund(refundAmount);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.ErrorCode);
        }
    }
}
