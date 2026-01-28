using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Concordia;
using Cobryx.Domain.Common;

namespace Cobryx.Application.Financial.Queries.GetPaymentMethods;

public record GetPaymentMethodsQuery() : IRequest<Result<IReadOnlyList<PaymentMethodDto>>>;

public record PaymentMethodDto(Guid Id, string Name, string Code, string? Description);

public class GetPaymentMethodsHandler : IRequestHandler<GetPaymentMethodsQuery, Result<IReadOnlyList<PaymentMethodDto>>>
{
    private readonly IPaymentMethodRepository _paymentMethodRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetPaymentMethodsHandler(
        IPaymentMethodRepository paymentMethodRepository,
        ITenantProvider tenantProvider)
    {
        _paymentMethodRepository = paymentMethodRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<IReadOnlyList<PaymentMethodDto>>("Tenant context missing.");

        var methods = await _paymentMethodRepository.GetAllActiveAsync(tenantId.Value);

        var dtos = methods.Select(m => new PaymentMethodDto(
            m.Id,
            m.Name,
            m.Code,
            m.Description
        )).ToList();

        return Result.Success<IReadOnlyList<PaymentMethodDto>>(dtos);
    }
}
