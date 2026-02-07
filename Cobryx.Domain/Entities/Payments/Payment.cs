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
    public Money RefundedAmount { get; private set; }
    public Money RefundableAmount => new Money(Amount.Amount - RefundedAmount.Amount, Amount.Currency);
    public bool IsFullyRefunded => RefundedAmount.Amount >= Amount.Amount;

    private readonly List<PaymentAllocation> _allocations = new();
    public IReadOnlyCollection<PaymentAllocation> Allocations => _allocations.AsReadOnly();

    public virtual PaymentMethod PaymentMethod { get; private set; } = null!;
    public virtual Customer Customer { get; private set; } = null!;

    private Payment()
    {
        Amount = null!;
        RefundedAmount = null!;
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
        RefundedAmount = Money.Zero(amount.Currency);
    }

    public void Initiate()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentNotPending);

        Status = PaymentStatus.Processing;
        UpdateTimestamp();
    }

    public void Complete()
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentNotProcessing);

        Status = PaymentStatus.Completed;

        var allocations = _allocations.Select(a => new PaymentAllocationEventData(a.InvoiceId, a.Amount)).ToList();
        AddDomainEvent(new PaymentCompletedEvent(Id, TenantId, CustomerId, Amount, allocations, DateTime.UtcNow));

        UpdateTimestamp();
    }

    public void Fail(string reason)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentNotProcessing);

        Status = PaymentStatus.Failed;
        AddDomainEvent(new PaymentFailedEvent(Id, TenantId, CustomerId, reason, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void AddAllocation(Guid invoiceId, Money amount)
    {
        if (amount.Amount <= 0) throw new DomainException(DomainErrorCode.Invoicing.InvalidAllocationAmount);
        _allocations.Add(new PaymentAllocation(Id, invoiceId, amount));
    }

    public void Cancel()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentCannotCancel);

        Status = PaymentStatus.Cancelled;
        UpdateTimestamp();
    }

    public void Refund(Money amount)
    {
        if (Status != PaymentStatus.Completed)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentNotCompletedCannotRefund);

        if (amount.Amount > RefundableAmount.Amount)
            throw new DomainException(DomainErrorCode.Invoicing.PaymentInsufficientRefundableAmount);

        RefundedAmount = new Money(RefundedAmount.Amount + amount.Amount, Amount.Currency);

        AddDomainEvent(new PaymentRefundedEvent(Id, TenantId, amount, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Chargeback()
    {
        if (Status == PaymentStatus.Chargeback) return;

        if (Status is not (PaymentStatus.Completed or PaymentStatus.Processing))
            throw new DomainException(DomainErrorCode.Invoicing.PaymentInvalidStatusForChargeback);

        Status = PaymentStatus.Chargeback;
        RefundedAmount = Amount;

        // In a chargeback, we implicitly reverse all allocations
        foreach (var allocation in _allocations)
        {
            allocation.MarkAsReversed();
        }

        AddDomainEvent(new PaymentChargebackedEvent(Id, TenantId, DateTime.UtcNow));
        UpdateTimestamp();
    }
}
