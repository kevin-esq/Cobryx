using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.ValueObjects;
using Concordia;

namespace Cobryx.Application.Tenants.Queries.GetBusinessSettings;

public record BusinessSettingsDto(
    string InterestType,
    decimal DefaultInterestValue,
    string PenaltyType,
    decimal PenaltyValue,
    bool AllowPartialPayments,
    string PaymentPriority,
    int GraceDays,
    decimal MinimumPaymentAmount);

public record GetBusinessSettingsQuery : IRequest<Result<BusinessSettingsDto>>;

public class GetBusinessSettingsHandler : IRequestHandler<GetBusinessSettingsQuery, Result<BusinessSettingsDto>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly ITenantProvider _tenantProvider;

    public GetBusinessSettingsHandler(ITenantRepository tenantRepository, ITenantProvider tenantProvider)
    {
        _tenantRepository = tenantRepository;
        _tenantProvider = tenantProvider;
    }

    public async Task<Result<BusinessSettingsDto>> Handle(GetBusinessSettingsQuery request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantProvider.GetTenantId();
        if (!tenantId.HasValue) return Result.Failure<BusinessSettingsDto>("Tenant context missing.");

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null) return Result.Failure<BusinessSettingsDto>(DomainErrorCode.Tenant.NotFound);

        var s = tenant.Settings;
        return Result.Success(new BusinessSettingsDto(
            s.InterestType.ToString(),
            s.DefaultInterestValue,
            s.PenaltyType.ToString(),
            s.PenaltyValue,
            s.AllowPartialPayments,
            s.PaymentPriority.ToString(),
            s.GraceDays,
            s.MinimumPaymentAmount));
    }
}
