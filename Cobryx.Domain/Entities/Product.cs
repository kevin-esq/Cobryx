using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Product : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Money BasePrice { get; private set; }
    public string? Sku { get; private set; }
    public bool IsService { get; private set; }
    public bool IsLoanProduct { get; private set; }


    public decimal? DefaultInterestRate { get; private set; }
    public int? MaxInstallments { get; private set; }

    public bool IsActive { get; private set; }

    private Product()
    {
        Name = null!;
        BasePrice = null!;
    }

    public Product(Guid tenantId, string name, Money basePrice, bool isService = false, bool isLoanProduct = false)
    {
        if (tenantId == Guid.Empty) throw new DomainException("DOMAIN.TENANT_ID_REQUIRED");
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("DOMAIN.NAME_REQUIRED");


        TenantId = tenantId;
        Name = name;
        BasePrice = basePrice;
        IsService = isService;
        IsLoanProduct = isLoanProduct;
        IsActive = true;
    }

    public void ConfigureLoanRules(decimal? defaultInterestRate, int? maxInstallments)
    {
        if (!IsLoanProduct) throw new DomainException("DOMAIN.NOT_A_LOAN_PRODUCT");
        DefaultInterestRate = defaultInterestRate;
        MaxInstallments = maxInstallments;
        UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? description, Money basePrice, string? sku)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new DomainException("DOMAIN.NAME_REQUIRED");
        if (basePrice == null) throw new DomainException("DOMAIN.BASE_PRICE_REQUIRED");

        Name = name;
        Description = description;
        BasePrice = basePrice;
        Sku = sku;
        UpdateTimestamp();
    }
}
