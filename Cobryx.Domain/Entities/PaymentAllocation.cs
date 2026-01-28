using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class PaymentAllocation : BaseEntity
{
    public Guid PaymentId { get; private set; }
    public Guid InvoiceId { get; private set; }
    public Money Amount { get; private set; }

    private PaymentAllocation() 
    { 
        Amount = null!;
    }

    public PaymentAllocation(Guid paymentId, Guid invoiceId, Money amount)
    {
        PaymentId = paymentId;
        InvoiceId = invoiceId;
        Amount = amount;
    }
}
