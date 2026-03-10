using Cobryx.Domain.Common;
using Cobryx.Domain.Entities.Lending.Enums;
using Cobryx.Domain.Entities.Payments;
using Cobryx.Domain.Exceptions;

namespace Cobryx.Domain.Entities.Lending;

/// <summary>
/// Domain aggregate root representing an active loan and its current financial state.
/// </summary>
public class Loan : BaseEntity, IAggregateRoot, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid LoanAgreementId { get; private set; }
    public string LoanNumber { get; private set; } = string.Empty;
    public LoanStatus Status { get; private set; }
    public LegalStatus LegalStatus { get; private set; }
    public RiskStatus RiskStatus { get; private set; }
    public CollectionStage CollectionStage { get; private set; }
    public decimal OriginalPrincipal { get; private set; }
    public decimal CurrentPrincipalBalance { get; private set; }
    public decimal CurrentInterestBalance { get; private set; }
    public decimal CurrentLateFeeBalance { get; private set; }
    public decimal TotalPaid { get; private set; }
    public DateTime? LastPaymentDate { get; private set; }
    public int DaysInArrears { get; private set; }
    public int FinancialDaysPastDue { get; private set; }
    public decimal ArrearsAmount { get; private set; }
    public FinancialStatus FinancialStatus { get; private set; }
    public DateTime? NextPaymentDueDate { get; private set; }
    public DateTime LastAccrualDate { get; private set; }
    public DateTime DisbursementDate { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public DateTime? ClosedDate => ClosedAt;
    public bool IsDemo { get; private set; }
    public bool IsWrittenOff { get; private set; }
    public DateTime? WriteOffDate { get; private set; }

    public virtual LoanAgreement Agreement { get; private set; } = null!;
    public virtual LoanDelinquencyState? DelinquencyState { get; private set; }
    public virtual ICollection<LoanCollectionsEvent> CollectionsEvents { get; private set; } = new List<LoanCollectionsEvent>();

    private readonly List<AccruedCharge> _accruedCharges = new();
    public IReadOnlyCollection<AccruedCharge> AccruedCharges => _accruedCharges.AsReadOnly();

    private readonly List<Installment> _installments = new();
    public IReadOnlyCollection<Installment> Installments => _installments.AsReadOnly();

    private readonly List<Payment> _payments = new();
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    private Loan() { }

    public Loan(
        Guid tenantId,
        Guid customerId,
        Guid loanAgreementId,
        string loanNumber,
        decimal principalAmount,
        bool isDemo = false)
    {
        if (tenantId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Common.TenantIdRequired);
        if (customerId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Customer.CustomerIdRequired);
        if (loanAgreementId == Guid.Empty)
            throw new DomainException(DomainErrorCode.Loans.AgreementNotFound);
        if (string.IsNullOrWhiteSpace(loanNumber))
            throw new DomainException(DomainErrorCode.Common.EntityNameRequired);

        TenantId = tenantId;
        CustomerId = customerId;
        LoanAgreementId = loanAgreementId;
        LoanNumber = loanNumber;
        OriginalPrincipal = principalAmount;
        CurrentPrincipalBalance = principalAmount;
        IsDemo = isDemo;
        CurrentInterestBalance = 0;
        CurrentLateFeeBalance = 0;
        TotalPaid = 0;
        DaysInArrears = 0;

        Status = LoanStatus.Draft;
        LegalStatus = LegalStatus.Active;
        RiskStatus = RiskStatus.OnTime;
        FinancialStatus = FinancialStatus.Current;
        CollectionStage = CollectionStage.None;
        ArrearsAmount = 0;
        FinancialDaysPastDue = 0;

        AddDomainEvent(new Events.Lending.LoanCreatedEvent(Id, TenantId, CustomerId, DateTime.UtcNow));
    }

    public void Activate()
    {
        if (Status != LoanStatus.Draft)
            return;

        Status = LoanStatus.Active;
        UpdateTimestamp();
    }

    public void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Any())
            throw new DomainException(DomainErrorCode.Credits.InstallmentsAlreadyGenerated);

        _installments.AddRange(installments);

        var firstPending = _installments
            .Where(i => i.Status == InstallmentStatus.Pending)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        NextPaymentDueDate = firstPending?.DueDate;
    }

    public void RecordPaymentApplied(decimal amount, DateTime paymentDate)
    {
        if (amount <= 0)
            throw new DomainException(DomainErrorCode.Loans.InvalidPaymentAmount);

        if (Status == LoanStatus.Closed)
            throw new DomainException(DomainErrorCode.Loans.AlreadyClosed);

        TotalPaid += amount;
        LastPaymentDate = paymentDate;
        UpdateTimestamp();
    }

    public void RecalculateBalances()
    {
        CurrentPrincipalBalance = _installments
            .Sum(i => i.PrincipalAmount - i.PrincipalPaid);

        CurrentInterestBalance = _installments
            .Sum(i => i.InterestAmount - i.InterestPaid);

        var nextPending = _installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        NextPaymentDueDate = nextPending?.DueDate;

        if (_installments.All(i => i.Status == InstallmentStatus.Paid))
        {
            MarkAsClosed();
        }

        UpdateTimestamp();
    }

    public void UpdateFinancialRiskStatus(DateTime today)
    {
        // Bank-Grade DPD: Today - Oldest Unpaid Installment DueDate
        var oldestDelinquent = _installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < today.Date)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        if (oldestDelinquent == null)
        {
            FinancialDaysPastDue = 0;
            FinancialStatus = FinancialStatus.Current;
            ArrearsAmount = 0;
        }
        else
        {
            FinancialDaysPastDue = (int)(today.Date - oldestDelinquent.DueDate.Date).TotalDays;
            ArrearsAmount = _installments
                .Where(i => i.DueDate <= today.Date)
                .Sum(i => (i.PrincipalAmount + i.InterestAmount + i.LateFeeAmount) -
                          (i.PrincipalPaid + i.InterestPaid + i.LateFeePaid));

            FinancialStatus = FinancialDaysPastDue switch
            {
                <= 3 => FinancialStatus.Current,
                <= 30 => FinancialStatus.Late,
                <= 90 => FinancialStatus.Delinquent,
                <= 180 => FinancialStatus.Default,
                _ => FinancialStatus == FinancialStatus.Recovered ? FinancialStatus.Recovered : FinancialStatus.ChargedOff
            };
        }

        // Keep legacy RiskStatus synced for UI compatibility
        DaysInArrears = FinancialDaysPastDue;
        RiskStatus = FinancialStatus switch
        {
            FinancialStatus.Current => RiskStatus.OnTime,
            FinancialStatus.Late => RiskStatus.Late,
            FinancialStatus.Delinquent => RiskStatus.SevereLate,
            _ => RiskStatus.Critical
        };

        CollectionStage = RiskStatus switch
        {
            RiskStatus.OnTime => CollectionStage.None,
            RiskStatus.Late => CollectionStage.Friendly,
            RiskStatus.SevereLate => CollectionStage.Hard,
            RiskStatus.Critical => CollectionStage.Legal,
            _ => CollectionStage.None
        };

        UpdateTimestamp();
    }

    public void MarkAsWrittenOff(DateTime date)
    {
        IsWrittenOff = true;
        WriteOffDate = date;
        FinancialStatus = FinancialStatus.ChargedOff;
        UpdateTimestamp();
    }

    public void MarkAsChargedOff()
    {
        if (FinancialStatus == FinancialStatus.ChargedOff) return;

        FinancialStatus = FinancialStatus.ChargedOff;

        // Note: Default policy: Auto-close contractual status when charged off.
        // In bank-level real, this might be optional.
        Status = LoanStatus.Closed;
        LegalStatus = LegalStatus.Defaulted;

        UpdateTimestamp();
    }

    public void MarkAsRecovered()
    {
        if (FinancialStatus != FinancialStatus.ChargedOff) return;

        FinancialStatus = FinancialStatus.Recovered;
        UpdateTimestamp();
    }

    public void MarkAsDisputed()
    {
        if (Status == LoanStatus.Disputed) return;
        Status = LoanStatus.Disputed;
        UpdateTimestamp();
    }

    public void MarkAsClosed()
    {
        if (Status == LoanStatus.Closed)
            return;

        Status = LoanStatus.Closed;
        LegalStatus = LegalStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsDefaulted()
    {
        // Now handled by FinancialStatus/LegalStatus
        FinancialStatus = FinancialStatus.Default;
        LegalStatus = LegalStatus.Defaulted;
        UpdateTimestamp();
    }

    public void MarkAccrued(DateTime accrualDate)
    {
        if (accrualDate <= LastAccrualDate)
            return;

        LastAccrualDate = accrualDate.Date;
        UpdateTimestamp();
    }

    public void AddAccruedCharge(AccruedCharge charge)
    {
        if (charge.LoanId != Id)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        if (charge.AccrualDate <= LastAccrualDate && _accruedCharges.Any(c => c.AccrualDate == charge.AccrualDate && c.Type == charge.Type))
            throw new DomainException(DomainErrorCode.Loans.DuplicateAccrual);

        _accruedCharges.Add(charge);

        switch (charge.Type)
        {
            case ChargeType.OrdinaryInterest:
                CurrentInterestBalance += charge.Amount;
                OutstandingInterest += charge.Amount;
                break;
            case ChargeType.LateFee:
            case ChargeType.Penalty:
                CurrentLateFeeBalance += charge.Amount;
                OutstandingFees += charge.Amount;
                break;
        }

        MarkAccrued(charge.AccrualDate);
        UpdateTimestamp();
    }

    public void AssessLateFees(DateTime date, decimal amount)
    {
        if (amount <= 0) return;
        AddAccruedCharge(new AccruedCharge(Id, ChargeType.LateFee, amount, date));
    }

    public void ApplyAllocation(LoanPaymentAllocation allocation)
    {
        if (allocation.LoanId != Id)
            throw new DomainException(DomainErrorCode.Common.GeneralError);

        // Apply with 10-decimal precision
        OutstandingFees = Math.Max(0, OutstandingFees - allocation.FeesApplied);
        OutstandingInterest = Math.Max(0, OutstandingInterest - allocation.InterestApplied);
        CurrentPrincipalBalance = Math.Max(0, CurrentPrincipalBalance - allocation.PrincipalApplied);

        CurrentInterestBalance = OutstandingInterest;
        CurrentLateFeeBalance = OutstandingFees;

        TotalPaid += (allocation.FeesApplied + allocation.InterestApplied + allocation.PrincipalApplied);

        // If principal is zero and no other debt, close the loan
        if (CurrentPrincipalBalance == 0 && OutstandingInterest == 0 && OutstandingFees == 0)
        {
            MarkAsClosed();
        }

        UpdateTimestamp();
    }

    public decimal OutstandingInterest { get; private set; }
    public decimal OutstandingFees { get; private set; }
    public decimal OutstandingPrincipal => CurrentPrincipalBalance;
}
