using Cobryx.Domain.Events;
using Cobryx.Domain.Events.Payments;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Payments;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Lending;

public class Credit : BaseLendingInstrument
{
    public Guid? ProductId { get; private set; }
    public CreditStatus Status { get; private set; }

    public DateTime StartDate { get; private set; }

    public virtual ICollection<Payment> Payments { get; private set; } = [];

    private Credit() : base() { Principal = null!; }

    public Credit(
        Guid tenantId,
        Guid customerId,
        Money principal,
        decimal interestRate,
        InterestType interestType,
        PaymentFrequency frequency,
        int installmentsCount,
        int graceDays = 0,
        Guid? productId = null) : base(tenantId, customerId, principal, interestRate, interestType, frequency, installmentsCount, graceDays)
    {
        if (tenantId == Guid.Empty) throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (customerId == Guid.Empty) throw new DomainException(DomainErrorCode.Customer.CustomerIdRequired);
        if (installmentsCount <= 0) throw new DomainException(DomainErrorCode.Credits.InvalidInstallmentsCount);

        ProductId = productId;
        StartDate = DateTime.UtcNow;
        Status = CreditStatus.Active;

        AddDomainEvent(new CreditCreatedEvent(Id, TenantId, CustomerId, principal, StartDate));
    }

    public void ApplyPayment(Guid paymentId, Money amount)
    {
        if (amount.Amount <= 0) return;
        if (Status == CreditStatus.Paid) throw new DomainException(DomainErrorCode.Credits.CreditAlreadyPaid);

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

}
