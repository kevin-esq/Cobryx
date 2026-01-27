using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid CreditId { get; private set; }
    public Guid TenantId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public string? Reference { get; private set; } // Physical receipt #, bank ref, etc.
    public string? Notes { get; private set; }

    private Payment() 
    { 
        Amount = null!;
    }

    public Payment(Guid tenantId, Guid creditId, Money amount, DateTime paymentDate, string? reference = null, string? notes = null)
    {
        if (tenantId == Guid.Empty) throw new ArgumentException("TenantId is required.");
        if (creditId == Guid.Empty) throw new ArgumentException("CreditId is required.");
        if (amount.Amount <= 0) throw new ArgumentException("Payment amount must be positive.");

        TenantId = tenantId;
        CreditId = creditId;
        Amount = amount;
        PaymentDate = paymentDate;
        Reference = reference;
        Notes = notes;
    }
}
