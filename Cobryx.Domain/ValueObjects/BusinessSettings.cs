using System.Collections.Generic;
using Cobryx.Domain.Common;

namespace Cobryx.Domain.ValueObjects;

public enum InterestType { Percentage, Fixed }
public enum PenaltyType { Daily, FixedOneTime }
public enum PaymentPriority { InterestFirst, CapitalFirst, Proportional }

public class BusinessSettings : ValueObject
{
    public InterestType InterestType { get; private set; }
    public decimal DefaultInterestValue { get; private set; }
    public PenaltyType PenaltyType { get; private set; }
    public decimal PenaltyValue { get; private set; }
    public bool AllowPartialPayments { get; private set; }
    public PaymentPriority PaymentPriority { get; private set; }
    public int GraceDays { get; private set; }
    public decimal MinimumPaymentAmount { get; private set; }

    // Private constructor for EF Core or deserialization
    private BusinessSettings() { }

    public BusinessSettings(
        InterestType interestType,
        decimal defaultInterestValue,
        PenaltyType penaltyType,
        decimal penaltyValue,
        bool allowPartialPayments,
        PaymentPriority paymentPriority,
        int graceDays,
        decimal minimumPaymentAmount)
    {
        InterestType = interestType;
        DefaultInterestValue = defaultInterestValue;
        PenaltyType = penaltyType;
        PenaltyValue = penaltyValue;
        AllowPartialPayments = allowPartialPayments;
        PaymentPriority = paymentPriority;
        GraceDays = graceDays;
        MinimumPaymentAmount = minimumPaymentAmount;
    }

    public static BusinessSettings Default()
    {
        return new BusinessSettings(
            InterestType.Percentage,
            10m, // 10% default
            PenaltyType.FixedOneTime,
            0m,
            true,
            PaymentPriority.InterestFirst,
            0,
            0m
        );
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return InterestType;
        yield return DefaultInterestValue;
        yield return PenaltyType;
        yield return PenaltyValue;
        yield return AllowPartialPayments;
        yield return PaymentPriority;
        yield return GraceDays;
        yield return MinimumPaymentAmount;
    }
}
