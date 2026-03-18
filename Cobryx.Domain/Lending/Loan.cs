using Cobryx.Domain.Events.Lending;
using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Domain aggregate root representing an active loan and its current financial state.
/// </summary>
public class Loan : BaseLendingInstrument, IAggregateRoot
{
    public Guid LoanAgreementId { get; private set; }
    public string LoanNumber { get; private set; } = string.Empty;
    public LoanStatus Status { get; private set; }
    public LegalStatus LegalStatus { get; private set; }
    public RiskStatus RiskStatus { get; private set; }
    public CollectionStage CollectionStage { get; private set; }

    public decimal OriginalPrincipal => Principal.Amount;
    public decimal OutstandingPrincipal => CurrentPrincipalBalance;
    public decimal OutstandingFees => CurrentLateFeeBalance;
    public decimal OutstandingInterest => CurrentInterestBalance;
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
    public virtual ICollection<AccruedCharge> AccruedCharges { get; private set; } = [];

    private Loan() : base() { }

    public Loan(
        Guid tenantId,
        Guid customerId,
        Guid loanAgreementId,
        string loanNumber,
        Money originalPrincipal,
        decimal interestRate = 0,
        InterestType interestType = InterestType.Simple,
        PaymentFrequency frequency = PaymentFrequency.Monthly,
        int installmentsCount = 0,
        int graceDays = 0,
        bool isDemo = false)
        : base(tenantId, customerId, originalPrincipal, interestRate, interestType, frequency, installmentsCount, graceDays)
    {
        LoanAgreementId = loanAgreementId;
        LoanNumber = loanNumber;
        Status = LoanStatus.Draft;
        LegalStatus = LegalStatus.Active;
        RiskStatus = RiskStatus.OnTime;
        CollectionStage = CollectionStage.None;
        CurrentPrincipalBalance = originalPrincipal.Amount;
        FinancialStatus = FinancialStatus.Current;
        LastAccrualDate = DateTime.UtcNow;
        IsDemo = isDemo;
    }

    public void MarkAsClosed()
    {
        Status = LoanStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        AddDomainEvent(new LoanStatusChangedEvent(Id, 0, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Activate()
    {
        if (Status != LoanStatus.Draft)
            return;
        Status = LoanStatus.Active;
        AddDomainEvent(new LoanStatusChangedEvent(Id, 0, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void Disburse(DateTime date)
    {
        Status = LoanStatus.Active;
        DisbursementDate = date;
        AddDomainEvent(new LoanDisbursedEvent(Id, 0, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public override void AddInstallments(IEnumerable<Installment> installments)
    {
        if (_installments.Count > 0)
            throw new DomainException(DomainErrorCode.Credits.InstallmentsAlreadyGenerated);

        base.AddInstallments(installments);

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
        AddDomainEvent(new LoanPaymentAppliedEvent(Id, 0, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void AssessLateFees(decimal amount)
    {
        CurrentLateFeeBalance += amount;
        UpdateTimestamp();
    }

    public void UpdateFinancialRiskStatus(DateTime? date = null)
    {
        var now = date ?? DateTime.UtcNow;
        var daysLate = 0;
        var overdueInstallments = _installments
            .Where(i => i.Status != InstallmentStatus.Paid && i.DueDate < now)
            .ToList();

        if (overdueInstallments.Count != 0)
        {
            daysLate = (int)(now - overdueInstallments.Min(i => i.DueDate)).TotalDays;
            FinancialStatus = daysLate > 90 ? FinancialStatus.Default : FinancialStatus.Late;
        }
        else
        {
            FinancialStatus = FinancialStatus.Current;
        }

        FinancialDaysPastDue = daysLate;
        ArrearsAmount = overdueInstallments.Sum(i => i.TotalDue.Amount);
        UpdateTimestamp();
    }

    public void MarkAsChargedOff()
    {
        FinancialStatus = FinancialStatus.ChargedOff;
        Status = LoanStatus.Closed;
        IsWrittenOff = true;
        WriteOffDate = DateTime.UtcNow;
        UpdateTimestamp();
    }

    public void MarkAsWrittenOff(DateTime date)
    {
        Status = LoanStatus.Closed;
        FinancialStatus = FinancialStatus.ChargedOff;
        IsWrittenOff = true;
        WriteOffDate = date;
        UpdateTimestamp();
    }

    public void AddAccruedCharge(AccruedCharge charge)
    {
        AccruedCharges.Add(charge);
        if (charge.Type == ChargeType.OrdinaryInterest)
        {
            CurrentInterestBalance += charge.Amount;
        }
        else if (charge.Type == ChargeType.LateFee)
        {
            CurrentLateFeeBalance += charge.Amount;
        }
        UpdateTimestamp();
    }

    public void MarkAccrued(DateTime date)
    {
        LastAccrualDate = date;
        AddDomainEvent(new AccrualPostedEvent(Id, 0, DateTime.UtcNow));
        UpdateTimestamp();
    }

    public void ApplyAllocation(LoanPaymentAllocation allocation)
    {
        RecordPaymentApplied(allocation.TotalApplied, allocation.AllocationDate);
        RecalculateBalances();
    }

    public void RecalculateBalances()
    {
        CurrentPrincipalBalance = _installments
            .Sum(i => i.PrincipalPart.Amount - i.PrincipalPaid.Amount);

        CurrentInterestBalance = _installments
            .Sum(i => i.InterestPart.Amount - i.InterestPaid.Amount);

        var nextPending = _installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .FirstOrDefault();

        NextPaymentDueDate = nextPending?.DueDate;

        if (_installments.Count > 0 && _installments.All(i => i.Status == InstallmentStatus.Paid))
        {
            MarkAsClosed();
        }
    }

    public void MarkAsRecovered()
    {
        FinancialStatus = FinancialStatus.Recovered;
        UpdateTimestamp();
    }

    public void MarkAsDisputed()
    {
        Status = LoanStatus.Disputed;
        UpdateTimestamp();
    }
}
