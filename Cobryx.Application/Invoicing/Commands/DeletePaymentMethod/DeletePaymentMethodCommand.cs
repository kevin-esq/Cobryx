using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.DeletePaymentMethod;

public record DeletePaymentMethodCommand(Guid Id) : IRequest<Result>;

public class DeletePaymentMethodHandler : IRequestHandler<DeletePaymentMethodCommand, Result>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantProvider _tenantProvider;

    public DeletePaymentMethodHandler(
        IPaymentMethodRepository paymentMethodRepository,
        ITenantProvider tenantProvider)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue)
            return Result.Failure(DomainErrorCode.Tenant.ContextMissing);

        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.Id, cancellationToken);
        if (paymentMethod == null || paymentMethod.TenantId != tenantId.Value)
            return Result.Failure(DomainErrorCode.Invoicing.PaymentMethodNotFound);

        paymentMethod.Delete();
        await _paymentMethodRepository.UpdateAsync(paymentMethod, cancellationToken);

        return Result.Success();
    }
}
