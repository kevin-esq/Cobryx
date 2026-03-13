using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
namespace Cobryx.Domain.Lending;

/// <summary>
/// Configurable late fee calculation policy.
/// </summary>
public class LateFeePolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public LateFeeType Type { get; private set; }
    public decimal? Amount { get; private set; }
    public decimal? PercentageRate { get; private set; }
    public int GracePeriodDays { get; private set; }
    public decimal? MaxLateFeeAmount { get; private set; }
    public bool IsActive { get; private set; }

    private LateFeePolicy() { }

    public static LateFeePolicy CreateFixed(
        Guid tenantId,
        string name,
        decimal amount,
        int gracePeriodDays = 0,
        decimal? maxLateFeeAmount = null)
    {
        ValidateCommon(tenantId, name);
        if (amount <= 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);

        return new LateFeePolicy
        {
            TenantId = tenantId,
            Name = name,
            Type = LateFeeType.Fixed,
            Amount = amount,
            GracePeriodDays = gracePeriodDays,
            MaxLateFeeAmount = maxLateFeeAmount,
            IsActive = true
        };
    }

    public static LateFeePolicy CreateDailyFlat(
        Guid tenantId,
        string name,
        decimal dailyAmount,
        int gracePeriodDays = 0,
        decimal? maxLateFeeAmount = null)
    {
        ValidateCommon(tenantId, name);
        if (dailyAmount <= 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);

        return new LateFeePolicy
        {
            TenantId = tenantId,
            Name = name,
            Type = LateFeeType.DailyFlat,
            Amount = dailyAmount,
            GracePeriodDays = gracePeriodDays,
            MaxLateFeeAmount = maxLateFeeAmount,
            IsActive = true
        };
    }

    public static LateFeePolicy CreateDailyPercentage(
        Guid tenantId,
        string name,
        decimal dailyPercentageRate,
        int gracePeriodDays = 0,
        decimal? maxLateFeeAmount = null)
    {
        ValidateCommon(tenantId, name);
        if (dailyPercentageRate <= 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);

        return new LateFeePolicy
        {
            TenantId = tenantId,
            Name = name,
            Type = LateFeeType.DailyPercentage,
            PercentageRate = dailyPercentageRate,
            GracePeriodDays = gracePeriodDays,
            MaxLateFeeAmount = maxLateFeeAmount,
            IsActive = true
        };
    }

    private static void ValidateCommon(Guid tenantId, string name)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);
    }

    public decimal CalculateLateFee(int daysLate, decimal outstandingBalance)
    {
        if (daysLate <= GracePeriodDays)
            return 0;

        var effectiveDaysLate = daysLate - GracePeriodDays;
        decimal fee = Type switch
        {
            LateFeeType.Fixed => Amount ?? 0,
            LateFeeType.DailyFlat => (Amount ?? 0) * effectiveDaysLate,
            LateFeeType.DailyPercentage => outstandingBalance * ((PercentageRate ?? 0) / 100) * effectiveDaysLate,
            _ => 0
        };

        if (MaxLateFeeAmount.HasValue && fee > MaxLateFeeAmount.Value)
            fee = MaxLateFeeAmount.Value;

        return fee;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void Activate()
    {
        IsActive = true;
        UpdateTimestamp();
    }
}
