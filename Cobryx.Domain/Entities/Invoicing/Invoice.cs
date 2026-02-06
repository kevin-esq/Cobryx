using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Events.Invoicing;

namespace Cobryx.Domain.Entities.Invoicing;

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

    private readonly List<Guid> _appliedPaymentIds = new();
    public IReadOnlyCollection<Guid> AppliedPaymentIds => _appliedPaymentIds.AsReadOnly();

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

    public void ApplyPayment(Guid paymentId, Money amount)
    {
        if (Status is not (InvoiceStatus.Issued or InvoiceStatus.Partial))
            throw new DomainException(DomainErrorCodes.Financial.InvoiceInvalidStatusForPayment);

        if (_appliedPaymentIds.Contains(paymentId))
            return;

        if (amount.Currency != Total.Currency)
            throw new DomainException(DomainErrorCodes.Financial.InvoiceCurrencyMismatch);

        decimal newPaid = TotalPaid.Amount + amount.Amount;
        TotalPaid = new Money(newPaid, Total.Currency);

        if (TotalPaid.Amount >= Total.Amount)
        {
            Status = InvoiceStatus.Paid;
            AddDomainEvent(new InvoicePaidEvent(Id, TenantId, CustomerId, DateTime.UtcNow));
        }
        else if (TotalPaid.Amount > 0)
        {
            Status = InvoiceStatus.Partial;
        }

        _appliedPaymentIds.Add(paymentId);
        UpdateTimestamp();
    }

    public void AddItem(string description, decimal quantity, decimal unitPrice, decimal taxRate, bool isTaxInclusive)
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("DOMAIN.INVOICE.NOT_DRAFT_ADD_ITEM");

        var item = new InvoiceItem(Id, description, quantity, unitPrice, taxRate, isTaxInclusive, Total.Currency);
        _items.Add(item);
        RecalculateTotals();
    }

    public void RecalculateTotals()
    {
        if (Status != InvoiceStatus.Draft)
            throw new DomainException("DOMAIN.INVOICE.NOT_DRAFT_RECALCULATE");

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
        if (Status != InvoiceStatus.Draft) throw new DomainException("DOMAIN.INVOICE.NOT_DRAFT");
        if (_items.Count == 0) throw new DomainException("DOMAIN.INVOICE.NO_ITEMS");

        Status = InvoiceStatus.Issued;
        AddDomainEvent(new InvoiceIssuedEvent(Id, TenantId, CustomerId, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Cancel()
    {
        if (Status is not (InvoiceStatus.Draft or InvoiceStatus.Issued))
            throw new DomainException("DOMAIN.INVOICE.INVALID_STATUS_FOR_CANCEL");

        Status = InvoiceStatus.Cancelled;
        UpdateTimestamp();
    }
}
