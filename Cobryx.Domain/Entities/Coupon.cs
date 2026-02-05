using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

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

    public bool IsValid()
    {
        if (!IsActive) return false;
        if (ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow) return false;
        if (MaxRedemptions.HasValue && TimesRedeemed >= MaxRedemptions.Value) return false;
        return true;
    }

    public void Redeem()
    {
        if (!IsValid()) throw new DomainException("DOMAIN.INVALID_COUPON");
        TimesRedeemed++;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }
}
