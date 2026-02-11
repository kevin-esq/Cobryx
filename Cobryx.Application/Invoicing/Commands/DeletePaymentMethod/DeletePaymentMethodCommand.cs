using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

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
        if (!tenantId.HasValue) return Result.Failure("TENANT.CONTEXT_MISSING");

        var paymentMethod = await _paymentMethodRepository.GetByIdAsync(request.Id);
        if (paymentMethod == null || paymentMethod.TenantId != tenantId.Value)
            return Result.Failure("PAYMENT_METHOD.NOT_FOUND");

        paymentMethod.Delete();
        await _paymentMethodRepository.UpdateAsync(paymentMethod);

        return Result.Success();
    }
}
