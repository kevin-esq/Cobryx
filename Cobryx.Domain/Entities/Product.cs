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
    public bool IsService { get; private set; } // True for "Cash Loans" or "Services"
    public bool IsLoanProduct { get; private set; } // If true, this product behaves as a template for a loan

    // Loan-specific configuration (overrides Tenant settings if present)
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
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.", nameof(tenantId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        // BasePrice can be 0 if the loan amount is variable

        TenantId = tenantId;
        Name = name;
        BasePrice = basePrice;
        IsService = isService;
        IsLoanProduct = isLoanProduct;
        IsActive = true;
    }

    public void ConfigureLoanRules(decimal? defaultInterestRate, int? maxInstallments)
    {
        if (!IsLoanProduct) throw new InvalidOperationException("Cannot configure loan rules for a non-loan product.");
        DefaultInterestRate = defaultInterestRate;
        MaxInstallments = maxInstallments;
        UpdateTimestamp();
    }

    public void UpdateDetails(string name, string? description, Money basePrice, string? sku)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (basePrice == null) throw new ArgumentNullException(nameof(basePrice));

        Name = name;
        Description = description;
        BasePrice = basePrice;
        Sku = sku;
        UpdateTimestamp();
    }
}
