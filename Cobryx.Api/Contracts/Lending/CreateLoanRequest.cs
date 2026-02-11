using Cobryx.Domain.Entities.Lending.Enums;

namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for creating a new loan with its execution policies.
/// </summary>
/// <remarks>
/// A **Loan** represents a fixed-term amortized agreement with a defined repayment schedule.
/// Unlike a Credit (revolving facility), a Loan generates a specific amortization schedule at creation.
///
/// Financial Precision:
/// - 'Amount' should be provided in the native currency unit (ISO-4217).
/// - Interest rates are applied per the policy referenced by 'InterestPolicyCode'.
/// - 'PaymentFrequency' determines the cadence of installment generation.
/// </remarks>
public record CreateLoanRequest(
    Guid CustomerId,
    decimal Amount,
    int InstallmentsCount,
    PaymentFrequency PaymentFrequency,
    string InterestPolicyCode,
    string PaymentApplicationPolicyCode,
    string? LateFeePolicyCode,
    LoanOrigin Origin,
    DateTime FirstDueDate,
    string? ReferenceId = null,
    string? Notes = null
);
