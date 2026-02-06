using Cobryx.Domain.Entities.Invoicing;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Events;
using Cobryx.Domain.Events.Payments;

namespace Cobryx.Domain.Entities;

public class Credit : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? ProductId { get; private set; }

    public Money Principal { get; private set; }
    public decimal InterestRate { get; private set; }
    public InterestType InterestType { get; private set; }
    public PaymentFrequency Frequency { get; private set; }
    public int InstallmentsCount { get; private set; }

    public DateTime StartDate { get; private set; }
    public int GraceDays { get; private set; }
    public CreditStatus Status { get; private set; }

    public virtual Customer Customer { get; private set; } = null!;
    public virtual ICollection<Payment> Payments { get; private set; } = new List<Payment>();

    private readonly List<Installment> _installments = new();
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    private Credit() { Principal = null!; }

    public Credit(
        Guid tenantId,
        Guid customerId,
        Money principal,
        decimal interestRate,
        InterestType interestType,
        PaymentFrequency frequency,
        int installmentsCount,
        int graceDays = 0,
        Guid? productId = null)
    {
        if (tenantId == Guid.Empty) throw new DomainException("DOMAIN.TENANT_ID_REQUIRED");
        if (customerId == Guid.Empty) throw new DomainException("DOMAIN.CUSTOMER_ID_REQUIRED");
        if (installmentsCount <= 0) throw new DomainException("DOMAIN.INVALID_INSTALLMENTS_COUNT");

        TenantId = tenantId;
        CustomerId = customerId;
        Principal = principal;
        InterestRate = interestRate;
        InterestType = interestType;
        Frequency = frequency;
        InstallmentsCount = installmentsCount;
        GraceDays = graceDays;
        ProductId = productId;

        StartDate = DateTime.UtcNow;
        Status = CreditStatus.Active;

        AddDomainEvent(new CreditCreatedEvent(Id, TenantId, CustomerId, Principal, StartDate));
    }

    public void ApplyPayment(Guid paymentId, Money amount)
    {
        if (amount.Amount <= 0) return;
        if (Status == CreditStatus.Paid) throw new DomainException("DOMAIN.CREDIT_ALREADY_PAID");

        decimal remainingAmount = amount.Amount;

        foreach (var installment in _installments.OrderBy(i => i.Number))
        {
            if (remainingAmount <= 0) break;
            if (installment.Status == InstallmentStatus.Paid) continue;

            remainingAmount = installment.ApplyPayment(remainingAmount);
        }

        if (_installments.All(i => i.Status == InstallmentStatus.Paid))
        {
            Status = CreditStatus.Paid;
        }

        AddDomainEvent(new PaymentAppliedEvent(Id, paymentId, amount, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Any()) throw new DomainException("DOMAIN.INSTALLMENTS_ALREADY_GENERATED");
        _installments.AddRange(installments);
    }
}
