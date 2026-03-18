using Cobryx.Domain.Lending.Enums;

namespace Cobryx.Domain.Lending;

public class PaymentApplicationService : IPaymentApplicationService
{
    public IReadOnlyList<PaymentAllocation> Apply(
        Loan loan,
        decimal amount,
        PaymentApplicationPolicy policy)
    {
        var allocations = new List<PaymentAllocation>();
        var remaining = amount;
        var applicationOrder = policy.GetApplicationOrder();

        foreach (var allocationType in applicationOrder)
        {
            if (remaining <= 0)
                break;

            var (allocationsBatch, remainingAfterType) = ApplyToType(loan, allocationType, remaining);
            allocations.AddRange(allocationsBatch);
            remaining = remainingAfterType;
        }

        if (remaining > 0)
        {
            allocations.Add(new PaymentAllocation(null, PaymentApplicationType.Principal, remaining));
        }

        return allocations;
    }

    private static (List<PaymentAllocation> Allocations, decimal Remaining) ApplyToType(
        Loan loan,
        PaymentApplicationType type,
        decimal amount)
    {
        var allocations = new List<PaymentAllocation>();
        var remaining = amount;

        switch (type)
        {
            case PaymentApplicationType.LateFees:
                if (loan.CurrentLateFeeBalance > 0)
                {
                    var toApply = Math.Min(remaining, loan.CurrentLateFeeBalance);
                    allocations.Add(new PaymentAllocation(null, PaymentApplicationType.LateFees, toApply));
                    remaining -= toApply;
                }
                break;

            case PaymentApplicationType.Interest:
                remaining = ApplyToInstallments(loan, PaymentApplicationType.Interest, remaining, allocations);
                break;

            case PaymentApplicationType.Principal:
                remaining = ApplyToInstallments(loan, PaymentApplicationType.Principal, remaining, allocations);
                break;
        }

        return (allocations, remaining);
    }

    private static decimal ApplyToInstallments(
        Loan loan,
        PaymentApplicationType type,
        decimal amount,
        List<PaymentAllocation> allocations)
    {
        var remaining = amount;
        var pendingInstallments = loan.Installments
            .Where(i => i.Status != InstallmentStatus.Paid)
            .OrderBy(i => i.DueDate)
            .ToList();

        foreach (var installment in pendingInstallments)
        {
            if (remaining <= 0)
                break;

            var available = type == PaymentApplicationType.Interest
                ? installment.InterestAmount - installment.InterestPaid.Amount
                : installment.PrincipalAmount - installment.PrincipalPaid.Amount;

            if (available > 0)
            {
                var toApply = Math.Min(remaining, available);
                installment.ApplyAllocation(toApply, type);
                allocations.Add(new PaymentAllocation(installment.Id, type, toApply));
                remaining -= toApply;
            }
        }

        return remaining;
    }
}
