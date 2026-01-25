using Cobryx.Domain.Common;
using Cobryx.Domain.Enums;
using Cobryx.Domain.ValueObjects;

namespace Cobryx.Domain.Entities;

public class Installment : BaseEntity
{
    public Guid CreditId { get; private set; }
    public int Number { get; private set; }
    public DateTime DueDate { get; private set; }
    public Money TotalAmount { get; private set; }
    public Money PrincipalPart { get; private set; }
    public Money InterestPart { get; private set; }
    public Money LateInterestAmount { get; private set; }
    public Money RemainingBalance { get; private set; }
    public InstallmentStatus Status { get; private set; }

    private Installment() { }

    public Installment(Guid creditId, int number, DateTime dueDate, Money principal, Money interest, Money balance)
    {
        CreditId = creditId;
        Number = number;
        DueDate = dueDate;
        PrincipalPart = principal;
        InterestPart = interest;
        LateInterestAmount = Money.Zero(principal.Currency); // Initialize as Zero
        TotalAmount = principal + interest;
        RemainingBalance = balance;
        Status = InstallmentStatus.Pending;
    }

    public void ApplyLateInterest(Money amount)
    {
        LateInterestAmount = amount;
        TotalAmount = PrincipalPart + InterestPart + LateInterestAmount;
        UpdateTimestamp();
    }

    public void MarkAsPaid()
    {
        Status = InstallmentStatus.Paid;
        UpdateTimestamp();
    }

    public void MarkAsOverdue()
    {
        if (Status == InstallmentStatus.Pending && DateTime.UtcNow > DueDate)
        {
            Status = InstallmentStatus.Overdue;
            UpdateTimestamp();
        }
    }
}
