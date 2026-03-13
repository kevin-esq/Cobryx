using Cobryx.Domain.Lending.Enums;

namespace Cobryx.Domain.Lending;

/// <summary>
/// Applies payments to loans following the configured allocation policy.
/// </summary>
public interface IPaymentApplicationService
{
    public IReadOnlyList<PaymentAllocation> Apply(
        Loan loan,
        decimal amount,
        PaymentApplicationPolicy policy);
}

/// <summary>
/// Result of applying a payment amount to a specific component.
/// </summary>
public record PaymentAllocation(
    Guid? InstallmentId,
    PaymentApplicationType Type,
    decimal Amount);
