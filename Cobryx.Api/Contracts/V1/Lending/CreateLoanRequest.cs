using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for creating a new loan with its execution policies.
/// </summary>
public record CreateLoanRequest(
    [Required] Guid CreditId,
    [Required] Guid CustomerId,
    [Required] decimal Amount,
    [Required] string Currency,
    [Required] string PaymentFrequency,
    [Required] int InstallmentsCount,
    [Required] string InterestPolicyCode,
    [Required] string LateFeePolicyCode,
    [Required] string PaymentApplicationPolicyCode,
    [Required] string Origin,
    [Required] DateTime StartDate,
    [Required] DateTime FirstDueDate,
    string? ReferenceId = null,
    string? Notes = null
)
{
    /// <summary>Identifier of the active credit facility to draw from.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CreditId { get; init; } = CreditId;

    /// <summary>Identifier of the customer receiving the loan.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>The amount to be disbursed in this loan.</summary>
    /// <example>10000.00</example>
    public decimal Amount { get; init; } = Amount;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Repayment frequency determining the billing cycle.</summary>
    /// <remarks>Valid values: Weekly, BiWeekly, Monthly</remarks>
    /// <example>Monthly</example>
    public string PaymentFrequency { get; init; } = PaymentFrequency;

    /// <summary>Total number of installments for the loan duration.</summary>
    /// <example>12</example>
    public int InstallmentsCount { get; init; } = InstallmentsCount;

    /// <summary>Defined policy for interest calculation logic.</summary>
    /// <remarks>Valid values: FIXED_PERCENTAGE, FLAT_FEE</remarks>
    /// <example>FIXED_PERCENTAGE</example>
    public string InterestPolicyCode { get; init; } = InterestPolicyCode;

    /// <summary>Defined policy for late fee penalties.</summary>
    /// <remarks>Valid values: STRICT_DAILY, GRACE_PERIOD_5_DAYS</remarks>
    /// <example>STRICT_DAILY</example>
    public string LateFeePolicyCode { get; init; } = LateFeePolicyCode;

    /// <summary>Defined policy for allocating payments across loan components.</summary>
    /// <remarks>Valid values: PRINCIPAL_FIRST, INTEREST_FIRST, PRO_RATA</remarks>
    /// <example>PRINCIPAL_FIRST</example>
    public string PaymentApplicationPolicyCode { get; init; } = PaymentApplicationPolicyCode;

    /// <summary>Origin identifier for the loan request.</summary>
    /// <remarks>Valid values: MOBILE, WEB, BRANCH</remarks>
    /// <example>MOBILE</example>
    public string Origin { get; init; } = Origin;

    /// <summary>Expected disbursement or start date.</summary>
    /// <example>2026-02-16T00:00:00Z</example>
    public DateTime StartDate { get; init; } = StartDate;

    /// <summary>The date when the first installment is due.</summary>
    /// <example>2026-03-16T00:00:00Z</example>
    public DateTime FirstDueDate { get; init; } = FirstDueDate;

    /// <summary>External reference or bridge identifier.</summary>
    /// <example>LN-91122</example>
    public string? ReferenceId { get; init; } = ReferenceId;

    /// <summary>Optional internal notes for the disbursement.</summary>
    /// <example>Direct bank transfer for business expansion.</example>
    public string? Notes { get; init; } = Notes;
}
