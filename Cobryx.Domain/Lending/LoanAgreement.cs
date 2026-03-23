using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
namespace Cobryx.Domain.Lending;

/// <summary>
/// Immutable contract representing what was agreed in a loan.
/// Once signed, this entity cannot be modified.
/// </summary>
public class LoanAgreement : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public decimal PrincipalAmount { get; private set; }
    public string Currency { get; private set; } = CobryxDefaults.Currency;
    public Guid InterestPolicyId { get; private set; }
    public PaymentFrequency PaymentFrequency { get; private set; }
    public int NumberOfInstallments { get; private set; }
    public int DaysBetweenPayments { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime FirstPaymentDate { get; private set; }
    public int GracePeriodDays { get; private set; }
    public Guid? LateFeePolicyId { get; private set; }
    public Guid PaymentApplicationPolicyId { get; private set; }
    public RoundingMode RoundingMode { get; private set; }
    public LoanOrigin Origin { get; private set; }
    public Guid? CreditSaleId { get; private set; }
    public string? ProductSnapshot { get; private set; }
    public bool IsRecoverable { get; private set; }
    public decimal? RecoveryValue { get; private set; }
    public bool IsSigned { get; private set; }
    public DateTime? SignedAt { get; private set; }

    public virtual Loan? Loan { get; private set; }
    public virtual InterestPolicy InterestPolicy { get; private set; } = null!;
    public virtual LateFeePolicy? LateFeePolicy { get; private set; }
    public virtual PaymentApplicationPolicy PaymentApplicationPolicy { get; private set; } = null!;

    private LoanAgreement() { }

    public LoanAgreement(
        Guid tenantId,
        Guid customerId,
        decimal principalAmount,
        Guid interestPolicyId,
        PaymentFrequency paymentFrequency,
        int numberOfInstallments,
        DateTime startDate,
        DateTime firstPaymentDate,
        LoanOrigin origin,
        int gracePeriodDays = 0,
        Guid? lateFeePolicyId = null,
        RoundingMode roundingMode = RoundingMode.ToNearest,
        Guid? creditSaleId = null,
        string? productSnapshot = null,
        bool isRecoverable = false,
        decimal? recoveryValue = null,
        int daysBetweenPayments = 0,
        string currency = CobryxDefaults.Currency,
        Guid? paymentApplicationPolicyId = null)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (customerId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Customer.CustomerIdRequired);
        if (principalAmount <= 0)
            throw new DomainException(DomainErrorCode.Common.InvalidAmount);
        if (numberOfInstallments <= 0)
            throw new DomainException(DomainErrorCode.Credits.InvalidInstallmentsCount);

        TenantId = tenantId;
        CustomerId = customerId;
        PrincipalAmount = principalAmount;
        Currency = currency;
        InterestPolicyId = interestPolicyId;
        PaymentFrequency = paymentFrequency;
        NumberOfInstallments = numberOfInstallments;
        DaysBetweenPayments = daysBetweenPayments;
        StartDate = startDate;
        FirstPaymentDate = firstPaymentDate;
        GracePeriodDays = gracePeriodDays;
        LateFeePolicyId = lateFeePolicyId;
        RoundingMode = roundingMode;
        Origin = origin;
        CreditSaleId = creditSaleId;
        ProductSnapshot = productSnapshot;
        IsRecoverable = isRecoverable;
        RecoveryValue = recoveryValue;
        PaymentApplicationPolicyId = paymentApplicationPolicyId ?? Guid.Empty;
        IsSigned = false;
    }

    public void Sign()
    {
        if (IsSigned)
            throw new DomainException(DomainErrorCode.Loans.AgreementAlreadySigned);

        IsSigned = true;
        SignedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public int GetDaysBetweenPayments()
    {
        return PaymentFrequency switch
        {
            PaymentFrequency.Weekly => 7,
            PaymentFrequency.BiWeekly => 14,
            PaymentFrequency.Monthly => 30,
            PaymentFrequency.Custom => DaysBetweenPayments,
            _ => 30
        };
    }
}
