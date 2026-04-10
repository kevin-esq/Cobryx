using Cobryx.Domain.Shared;
using Cobryx.Domain.Shared.Enums;

namespace Cobryx.Domain.Identity;

public class Coupon : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Code { get; private set; }
    public DiscountType DiscountType { get; private set; }
    public decimal Value { get; private set; }
    public DateTime? ValidUntil { get; private set; }
    public int? MaxRedemptions { get; private set; }
    public int TimesRedeemed { get; private set; }
    public bool IsActive { get; private set; }

    private Coupon()
    {
        Code = null!;
    }

    public Coupon(Guid tenantId, string code, DiscountType discountType, decimal value, DateTime? validUntil = null, int? maxRedemptions = null)
    {
        TenantId = tenantId;
        Code = code.ToUpperInvariant();
        DiscountType = discountType;
        Value = value;
        ValidUntil = validUntil;
        MaxRedemptions = maxRedemptions;
        TimesRedeemed = 0;
        IsActive = true;
    }

    public bool IsValid() => IsValid(DateTime.UtcNow);

    public bool IsValid(DateTime now)
    {
        if (!IsActive)
            return false;
        if (ValidUntil.HasValue && ValidUntil.Value < now)
            return false;
        if (MaxRedemptions.HasValue && TimesRedeemed >= MaxRedemptions.Value)
            return false;
        return true;
    }

    public void Redeem() => Redeem(DateTime.UtcNow);

    public void Redeem(DateTime now)
    {
        if (!IsValid(now))
            throw new DomainException(DomainErrorCode.Marketing.InvalidCouponGeneral);
        TimesRedeemed++;
        UpdateTimestamp(now);
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
