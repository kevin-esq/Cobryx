using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;

using Concordia;

namespace Cobryx.Application.Invoicing.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(
    string Name,
    string Code,
    string? Description = null) : IRequest<Result<Guid>>;

public class CreatePaymentMethodHandler(
    IPaymentMethodRepository paymentMethodRepository,
    ITenantProvider tenantProvider) : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository = paymentMethodRepository;
    private readonly ITenantProvider _tenantProvider = tenantProvider;

    public async Task<Result<Guid>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>(DomainErrorCode.Tenant.ContextMissing);

        var existing = await _paymentMethodRepository.GetByCodeAsync(tenantId.Value, request.Code, cancellationToken);
        if (existing != null)
            return Result.Failure<Guid>(DomainErrorCode.Invoicing.PaymentMethodAlreadyExists);

        var paymentMethod = new PaymentMethod(
            tenantId.Value,
            request.Name,
            request.Code,
            request.Description);

        await _paymentMethodRepository.AddAsync(paymentMethod, cancellationToken);

        return Result.Success(paymentMethod.Id);
    }
}
