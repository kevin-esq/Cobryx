using Cobryx.Domain.Lending.Enums;
using Cobryx.Domain.Shared;


namespace Cobryx.Domain.ValueObjects;

public record BusinessSettings(
    InterestType InterestType,
    decimal DefaultInterestValue,
    PenaltyType PenaltyType,
    decimal PenaltyValue,
    bool AllowPartialPayments,
    PaymentPriority PaymentPriority,
    int GraceDays,
    decimal MinimumPaymentAmount) : ValueObject
{
    public static BusinessSettings Default()
    {
        return new BusinessSettings(
            InterestType.Simple,
            10m,
            PenaltyType.FixedOneTime,
            0m,
            true,
            PaymentPriority.InterestFirst,
            0,
            0m
        );
    }

    protected override IEnumerable<object?> GetEqualityComponents()
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
