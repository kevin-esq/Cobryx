using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Entities;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Financial.Commands.CreatePaymentMethod;

public record CreatePaymentMethodCommand(
    string Name,
    string Code,
    string? Description = null) : IRequest<Result<Guid>>;

public class CreatePaymentMethodHandler : IRequestHandler<CreatePaymentMethodCommand, Result<Guid>>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantProvider _tenantProvider;

    public CreatePaymentMethodHandler(
        IPaymentMethodRepository paymentMethodRepository,
        ITenantProvider tenantProvider)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<Guid>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<Guid>("Tenant context missing.");

        var existing = await _paymentMethodRepository.GetByCodeAsync(tenantId.Value, request.Code);
        if (existing != null)
            return Result.Failure<Guid>($"Payment method with code {request.Code} already exists.");

        var paymentMethod = new PaymentMethod(
            tenantId.Value,
            request.Name,
            request.Code,
            request.Description);

        await _paymentMethodRepository.AddAsync(paymentMethod);

        return Result.Success(paymentMethod.Id);
    }
}
