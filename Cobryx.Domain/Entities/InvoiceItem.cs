using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class InvoiceItem : BaseEntity
{
    public Guid InvoiceId { get; private set; }
    public string Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxRate { get; private set; }
    public bool IsTaxInclusive { get; private set; }

    public Money Subtotal { get; private set; } = null!;
    public Money TaxAmount { get; private set; } = null!;
    public Money Total { get; private set; } = null!;

    private InvoiceItem()
    {
        Description = null!;
    }

    public InvoiceItem(Guid invoiceId, string description, decimal quantity, decimal unitPrice, decimal taxRate, bool isTaxInclusive, string currency)
    {
        InvoiceId = invoiceId;
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
        TaxRate = taxRate;
        IsTaxInclusive = isTaxInclusive;

        Calculate(currency);
    }

    private void Calculate(string currency)
    {
        var (subtotal, taxAmount, total) = Services.TaxCalculator.Calculate(Quantity, UnitPrice, TaxRate, IsTaxInclusive);

        Subtotal = new Money(subtotal, currency);
        TaxAmount = new Money(taxAmount, currency);
        Total = new Money(total, currency);
    }
}
