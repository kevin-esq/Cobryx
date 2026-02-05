using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;

namespace Cobryx.Domain.Entities;

public class Payment : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid PaymentMethodId { get; private set; }
    public Money Amount { get; private set; }
    public DateTime PaymentDate { get; private set; }
    public string? Reference { get; private set; }
    public string? Notes { get; private set; }
    public PaymentStatus Status { get; private set; }

    private readonly List<PaymentAllocation> _allocations = new();
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    public virtual PaymentMethod PaymentMethod { get; private set; } = null!;
    public virtual Customer Customer { get; private set; } = null!;

    private Payment()
    {
        Amount = null!;
    }

    public Payment(Guid tenantId, Guid customerId, Guid paymentMethodId, Money amount, DateTime paymentDate, string? reference = null, string? notes = null)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        PaymentMethodId = paymentMethodId;
        Amount = amount;
        PaymentDate = paymentDate;
        Reference = reference;
        Notes = notes;
        Status = PaymentStatus.Completed;
    }

    public void AddAllocation(Guid invoiceId, Money amount)
    {
        if (amount.Amount <= 0) throw new DomainException("DOMAIN.INVALID_ALLOCATION_AMOUNT");
        _allocations.Add(new PaymentAllocation(Id, invoiceId, amount));
    }

    public void Cancel()
    {
        Status = PaymentStatus.Cancelled;
        UpdateTimestamp();
    }
}
