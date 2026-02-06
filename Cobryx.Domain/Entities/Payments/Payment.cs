using Cobryx.Domain.Common;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Enums;
using Cobryx.Domain.Events.Payments;

namespace Cobryx.Domain.Entities.Payments;

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
        Status = PaymentStatus.Pending;
    }

    public void Initiate()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("DOMAIN.PAYMENT.NOT_PENDING");

        Status = PaymentStatus.Processing;
        UpdateTimestamp();
    }

    public void Complete()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException(DomainErrorCodes.Financial.PaymentNotProcessing);

        Status = PaymentStatus.Completed;

        var allocations = _allocations.Select(a => new PaymentAllocationEventData(a.InvoiceId, a.Amount)).ToList();
        AddDomainEvent(new PaymentCompletedEvent(Id, TenantId, CustomerId, Amount, allocations, DateTime.UtcNow));

        UpdateTimestamp();
    }

    public void Fail(string reason)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException("DOMAIN.PAYMENT.NOT_PROCESSING");

        Status = PaymentStatus.Failed;
        AddDomainEvent(new PaymentFailedEvent(Id, TenantId, CustomerId, reason, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void AddAllocation(Guid invoiceId, Money amount)
    {
        if (amount.Amount <= 0) throw new DomainException("DOMAIN.INVALID_ALLOCATION_AMOUNT");
        _allocations.Add(new PaymentAllocation(Id, invoiceId, amount));
    }

    public void Cancel()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException("DOMAIN.PAYMENT.CANNOT_CANCEL");

        Status = PaymentStatus.Cancelled;
        UpdateTimestamp();
    }
}
