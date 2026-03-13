using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;
using Cobryx.Domain.Lending.Enums;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Common base for all lending-related financial instruments that share an amortization schedule (Installments).
/// This unification allows EF Core to map Installments to a single logical parent hierarchy.
/// </summary>
public abstract class BaseLendingInstrument : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; protected set; }
    public Guid CustomerId { get; protected set; }
    public virtual Customer Customer { get; protected set; } = null!;

    public Money Principal { get; protected set; } = null!;
    public decimal InterestRate { get; protected set; }
    public InterestType InterestType { get; protected set; }
    public PaymentFrequency Frequency { get; protected set; }
    public int InstallmentsCount { get; protected set; }
    public int GraceDays { get; protected set; }

    protected readonly List<Installment> _installments = [];
    public virtual IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    protected BaseLendingInstrument() { }

    protected BaseLendingInstrument(
        Guid tenantId,
        Guid customerId,
        Money principal,
        decimal interestRate,
        InterestType interestType,
        PaymentFrequency frequency,
        int installmentsCount,
        int graceDays = 0)
    {
        TenantId = tenantId;
        CustomerId = customerId;
        Principal = principal;
        InterestRate = interestRate;
        InterestType = interestType;
        Frequency = frequency;
        InstallmentsCount = installmentsCount;
        GraceDays = graceDays;
    }

    public virtual void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Count > 0)
            throw new DomainException(DomainErrorCode.Credits.InstallmentsAlreadyGenerated);

        _installments.AddRange(installments);
    }
}
