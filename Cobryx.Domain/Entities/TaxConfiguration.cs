using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class TaxConfiguration : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public decimal Rate { get; private set; }
    public bool IsInclusive { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsDefault { get; private set; }

    private TaxConfiguration()
    {
        Name = null!;
    }

    public TaxConfiguration(Guid tenantId, string name, decimal rate, bool isInclusive = true, bool isDefault = false)
    {
        if (tenantId == Guid.Empty) throw new DomainException("DOMAIN.TENANT_ID_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("DOMAIN.TAX.NAME_REQUIRED");
        if (rate < 0) throw new DomainException("DOMAIN.TAX.RATE_NEGATIVE");

        TenantId = tenantId;
        Name = name;
        Rate = rate;
        IsInclusive = isInclusive;
        IsActive = true;
        IsDefault = isDefault;
    }

    public void Update(string name, decimal rate, bool isInclusive, bool isDefault)
    {
        Name = name;
        Rate = rate;
        IsInclusive = isInclusive;
        IsDefault = isDefault;
        UpdateTimestamp();
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdateTimestamp();
    }

    public void SetAsDefault()
    {
        IsDefault = true;
        UpdateTimestamp();
    }

    public void UnsetDefault()
    {
        IsDefault = false;
        UpdateTimestamp();
    }
}
