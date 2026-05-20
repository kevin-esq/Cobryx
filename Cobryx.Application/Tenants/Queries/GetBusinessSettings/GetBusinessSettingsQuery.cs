using Cobryx.Application.Common.Attributes;
using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Interfaces;
using Cobryx.Domain.Shared;

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
    decimal MinimumPaymentAmount
)
{
    /// <summary>Calculation method for late interests (PERCENTAGE, FIXED).</summary>
    /// <example>PERCENTAGE</example>
    public string InterestType { get; init; } = InterestType;

    /// <summary>Default interest rate or amount.</summary>
    /// <example>1.5</example>
    public decimal DefaultInterestValue { get; init; } = DefaultInterestValue;

    /// <summary>Calculation method for penalties (PERCENTAGE, FIXED).</summary>
    /// <example>FIXED</example>
    public string PenaltyType { get; init; } = PenaltyType;

    /// <summary>Default penalty rate or amount.</summary>
    /// <example>50.00</example>
    public decimal PenaltyValue { get; init; } = PenaltyValue;

    /// <summary>True if customers can pay less than the total due.</summary>
    /// <example>true</example>
    public bool AllowPartialPayments { get; init; } = AllowPartialPayments;

    /// <summary>Order of fund allocation (FIFO, OLDEST_FIRST).</summary>
    /// <example>OLDEST_FIRST</example>
    public string PaymentPriority { get; init; } = PaymentPriority;

    /// <summary>Days allowed before interest/penalties apply.</summary>
    /// <example>3</example>
    public int GraceDays { get; init; } = GraceDays;

    /// <summary>Minimum allow payment amount.</summary>
    /// <example>10.00</example>
    public decimal MinimumPaymentAmount { get; init; } = MinimumPaymentAmount;
}

[TenantScoped]
public record GetBusinessSettingsQuery : IRequest<Result<BusinessSettingsDto>>, IRequiresTenant;

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
        if (!tenantId.HasValue)
            return Result.Failure<BusinessSettingsDto>(DomainErrorCode.Tenant.ContextMissing);

        var tenant = await _tenantRepository.GetByIdAsync(tenantId.Value, cancellationToken);
        if (tenant == null)
            return Result.Failure<BusinessSettingsDto>(DomainErrorCode.Tenant.NotFound);

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
