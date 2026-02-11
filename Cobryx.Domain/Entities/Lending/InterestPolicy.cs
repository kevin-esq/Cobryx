using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Configurable interest calculation policy supporting explicit rates and implicit rates (credit sales).
/// </summary>
public class InterestPolicy : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public InterestOrigin Type { get; private set; }
    public InterestMethod Method { get; private set; }
    public decimal? Rate { get; private set; }
    public CompoundingFrequency? CompoundingFrequency { get; private set; }
    public decimal? CashPrice { get; private set; }
    public decimal? CreditPrice { get; private set; }
    public bool IsActive { get; private set; }

    private InterestPolicy() { }

    public static InterestPolicy CreateExplicit(
        Guid tenantId,
        string name,
        string code,
        decimal rate,
        InterestMethod method = InterestMethod.Simple,
        CompoundingFrequency? compoundingFrequency = null)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(DomainErrorCode.Common.GeneralError);
        if (rate < 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);

        return new InterestPolicy
        {
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            Type = InterestOrigin.Explicit,
            Method = method,
            Rate = rate,
            CompoundingFrequency = compoundingFrequency,
            IsActive = true
        };
    }

    public static InterestPolicy CreateImplicit(
        Guid tenantId,
        string name,
        string code,
        decimal cashPrice,
        decimal creditPrice)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException(DomainErrorCode.Common.GeneralError);
        if (cashPrice <= 0 || creditPrice <= 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);
        if (creditPrice < cashPrice)
            throw new DomainException(DomainErrorCode.Loans.CreditSaleInvalidPricing);

        return new InterestPolicy
        {
            TenantId = tenantId,
            Name = name,
            Code = code.ToUpperInvariant(),
            Type = InterestOrigin.Implicit,
            Method = InterestMethod.Simple,
            CashPrice = cashPrice,
            CreditPrice = creditPrice,
            IsActive = true
        };
    }

    public decimal CalculateInterest(decimal principal, int numberOfInstallments)
    {
        if (Type == InterestOrigin.Implicit)
            return CalculateImplicitInterest(principal);

        if (!Rate.HasValue)
            return 0;

        if (Method == InterestMethod.Simple)
            return principal * (Rate.Value / 100) * numberOfInstallments;

        var periodicRate = Rate.Value / 100;
        var compoundedAmount = principal * (decimal)Math.Pow((double)(1 + periodicRate), numberOfInstallments);
        return compoundedAmount - principal;
    }

    public decimal CalculateImplicitInterest(decimal financedAmount)
    {
        if (!CashPrice.HasValue || !CreditPrice.HasValue || CashPrice.Value <= 0)
            return 0;

        var interestRatio = (CreditPrice.Value - CashPrice.Value) / CashPrice.Value;
        return financedAmount * interestRatio;
    }

    public decimal CalculateImplicitRate()
    {
        if (!CashPrice.HasValue || !CreditPrice.HasValue || CashPrice.Value <= 0)
            return 0;

        return (CreditPrice.Value - CashPrice.Value) / CashPrice.Value * 100;
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
