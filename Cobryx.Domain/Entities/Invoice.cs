using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Invoice : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string InvoiceNumber { get; private set; }
    public DateTime IssueDate { get; private set; }
    public DateTime DueDate { get; private set; }
    public InvoiceStatus Status { get; private set; }

    public Money Subtotal { get; private set; }
    public Money TaxAmount { get; private set; }
    public Money Total { get; private set; }

    public Guid? InstallmentId { get; private set; }
    public Money TotalPaid { get; private set; }
    public string? Notes { get; private set; }

    private readonly List<InvoiceItem> _items = new();
    public IReadOnlyCollection<InvoiceItem> Items => _items.AsReadOnly();

    public virtual Customer Customer { get; private set; } = null!;
    public virtual Installment? Installment { get; private set; }

    private Invoice()
    {
        InvoiceNumber = null!;
        Subtotal = null!;
        TaxAmount = null!;
        Total = null!;
        TotalPaid = null!;
    }

    public Invoice(Guid tenantId, Guid customerId, string invoiceNumber, DateTime issueDate, DateTime dueDate, Guid? installmentId = null, string currency = "MXN")
    {
        TenantId = tenantId;
        CustomerId = customerId;
        InvoiceNumber = invoiceNumber;
        IssueDate = issueDate;
        DueDate = dueDate;
        InstallmentId = installmentId;
        Status = InvoiceStatus.Draft;
        Subtotal = Money.Zero(currency);
        TaxAmount = Money.Zero(currency);
        Total = Money.Zero(currency);
        TotalPaid = Money.Zero(currency);
    }

    public void ApplyPayment(Money amount)
    {
        if (amount.Currency != Total.Currency) throw new InvalidOperationException("Currency mismatch.");

        decimal newPaid = TotalPaid.Amount + amount.Amount;
        TotalPaid = new Money(newPaid, Total.Currency);

        if (TotalPaid.Amount >= Total.Amount)
            Status = InvoiceStatus.Paid;
        else if (TotalPaid.Amount > 0)
            Status = InvoiceStatus.Partial;

        UpdateTimestamp();
    }

    public void AddItem(string description, decimal quantity, decimal unitPrice, decimal taxRate, bool isTaxInclusive)
    {
        var item = new InvoiceItem(Id, description, quantity, unitPrice, taxRate, isTaxInclusive, Total.Currency);
        _items.Add(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        decimal subtotal = 0;
        decimal taxTotal = 0;

        foreach (var item in _items)
        {
            subtotal += item.Subtotal.Amount;
            taxTotal += item.TaxAmount.Amount;
        }

        Subtotal = new Money(subtotal, Total.Currency);
        TaxAmount = new Money(taxTotal, Total.Currency);
        Total = new Money(subtotal + taxTotal, Total.Currency);
        UpdateTimestamp();
    }

    public void Issue()
    {
        if (Status != InvoiceStatus.Draft) throw new InvalidOperationException("Only draft invoices can be issued.");
        Status = InvoiceStatus.Issued;
        UpdateTimestamp();
    }

    public void Cancel()
    {
        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }
}
