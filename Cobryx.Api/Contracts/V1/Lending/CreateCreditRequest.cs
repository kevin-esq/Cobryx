using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for creating a credit line facility.
/// </summary>
/// <remarks>
/// A **Credit** represents a revolving line of credit. Unlike a Loan (which is a fixed amortized agreement),
/// a credit facility defines the maximum borrowing capacity and terms under which draws may occur.
/// </remarks>
public record CreateCreditRequest(
    [Required] Guid CustomerId,
    [Required] decimal Amount,
    [Required] string Currency,
    [Required] decimal InterestRate,
    [Required] string InterestType,
    [Required] string Frequency,
    [Required] int InstallmentsCount,
    [Required] int GraceDays,
    Guid? ProductId = null
)
{
    /// <summary>Identifier of the customer receiving the credit.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>Total principal amount granted.</summary>
    /// <example>50000.00</example>
    public decimal Amount { get; init; } = Amount;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Intereste rate (0.05 = 5%).</summary>
    /// <example>0.08</example>
    public decimal InterestRate { get; init; } = InterestRate;

    /// <summary>Calculation method used for interest accrual.</summary>
    /// <remarks>Valid values: FIXED, PERCENTAGE</remarks>
    /// <example>PERCENTAGE</example>
    public string InterestType { get; init; } = InterestType;

    /// <summary>Payment recurrence/billing cycle.</summary>
    /// <remarks>Valid values: WEEKLY, BIWEEKLY, MONTHLY</remarks>
    /// <example>MONTHLY</example>
    public string Frequency { get; init; } = Frequency;

    /// <summary>Total number of payments to complete the facility.</summary>
    /// <example>12</example>
    public int InstallmentsCount { get; init; } = InstallmentsCount;

    /// <summary>Days allowed before interest applies.</summary>
    /// <example>5</example>
    public int GraceDays { get; init; } = GraceDays;

    /// <summary>Optional credit product template to inherit settings from.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa9</example>
    public Guid? ProductId { get; init; } = ProductId;
}
