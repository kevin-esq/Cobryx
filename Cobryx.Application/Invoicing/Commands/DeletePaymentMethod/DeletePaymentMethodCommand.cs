using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.DeletePaymentMethod;

[TenantScoped]
public record DeletePaymentMethodCommand(Guid Id) : IRequest<Result>, IRequiresTenant;

public class DeletePaymentMethodHandler(
    IPaymentMethodRepository paymentMethodRepository,
    ITenantProvider tenantProvider)
    : IRequestHandler<DeletePaymentMethodCommand, Result>
{
    public async Task<Result> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        Guid? tenantId = tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        PaymentMethod? paymentMethod = await paymentMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (paymentMethod == null || paymentMethod.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentMethodNotFound);

        paymentMethod.Delete();
        await paymentMethodRepository.UpdateAsync(paymentMethod, cancellationToken);

        return Result.Success();
    }
}
