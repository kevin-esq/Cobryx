using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Lending;

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
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Products.GenericNameRequired);


        TenantId = tenantId;
        Name = name;
        BasePrice = basePrice;
        IsService = isService;
        IsLoanProduct = isLoanProduct;
        IsActive = true;
    }

    public void ConfigureLoanRules(decimal? defaultInterestRate, int? maxInstallments)
    {
        if (!IsLoanProduct)
            throw new DomainException(DomainErrorCode.Products.NotALoanProduct);
        DefaultInterestRate = defaultInterestRate;
        MaxInstallments = maxInstallments;
        UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? description, Money basePrice, string? sku)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(DomainErrorCode.Products.GenericNameRequired);
        Name = name;
        Description = description;
        BasePrice = basePrice ?? throw new DomainException(DomainErrorCode.Products.BasePriceRequired);
        Sku = sku;
        UpdateTimestamp();
    }
}
