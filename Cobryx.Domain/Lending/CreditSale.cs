using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Represents a sale on credit (muebles, motos, electrodomésticos).
/// </summary>
public class CreditSale : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string ProductDescription { get; private set; } = string.Empty;
    public string? ProductCategory { get; private set; }
    public decimal CashPrice { get; private set; }
    public decimal CreditPrice { get; private set; }
    public decimal DownPayment { get; private set; }
    public decimal FinancedAmount => CreditPrice - DownPayment;
    public DateTime SaleDate { get; private set; }
    public Guid? LoanAgreementId { get; private set; }
    public Guid? LoanId { get; private set; }
    public Guid? DownPaymentId { get; private set; }

    public virtual LoanAgreement? Agreement { get; private set; }
    public virtual Loan? Loan { get; private set; }

    private CreditSale() { }

    public CreditSale(
        Guid tenantId,
        Guid customerId,
        string productDescription,
        decimal cashPrice,
        decimal creditPrice,
        decimal downPayment,
        DateTime saleDate,
        string? productCategory = null)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (customerId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Customer.CustomerIdRequired);
        if (string.IsNullOrWhiteSpace(productDescription))
            throw new DomainException(DomainErrorCode.Products.NameRequired);
        if (cashPrice <= 0)
            throw new DomainException(DomainErrorCode.Loans.CreditSaleInvalidPricing);
        if (creditPrice <= 0 || creditPrice < cashPrice)
            throw new DomainException(DomainErrorCode.Loans.CreditSaleInvalidPricing);
        if (downPayment < 0 || downPayment >= creditPrice)
            throw new DomainException(DomainErrorCode.Loans.CreditSaleInvalidPricing);

        TenantId = tenantId;
        CustomerId = customerId;
        ProductDescription = productDescription;
        ProductCategory = productCategory;
        CashPrice = cashPrice;
        CreditPrice = creditPrice;
        DownPayment = downPayment;
        SaleDate = saleDate;
    }

    public decimal CalculateImplicitInterestRate()
    {
        if (CashPrice <= 0)
            return 0;
        return (CreditPrice - CashPrice) / CashPrice * 100;
    }

    public void LinkLoan(Guid loanAgreementId, Guid loanId)
    {
        LoanAgreementId = loanAgreementId;
        LoanId = loanId;
        UpdateTimestamp();
    }

    public void LinkDownPayment(Guid paymentId)
    {
        DownPaymentId = paymentId;
        UpdateTimestamp();
    }

    public string GenerateProductSnapshot()
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            description = ProductDescription,
            category = ProductCategory,
            cashPrice = CashPrice,
            creditPrice = CreditPrice,
            saleDate = SaleDate.ToString("yyyy-MM-dd")
        });
    }
}
