using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Payments.Commands.HandleChargeback;

[TenantScoped]
public record HandleChargebackCommand(Guid PaymentId) : IRequest<Result>, IRequiresTenant;

public class HandleChargebackHandler : IRequestHandler<HandleChargebackCommand, Result>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITenantProvider _tenantProvider;
    private readonly IUnitOfWork _unitOfWork;

    public HandleChargebackHandler(
        IPaymentRepository paymentRepository,
        ITenantProvider tenantProvider,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _tenantProvider = tenantProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(HandleChargebackCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment == null)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentNotFound);

        if (payment.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        try
        {
            payment.Chargeback();
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (DomainException ex)
        {
            return Result.Failure(ex.ErrorCode);
        }
    }
}
