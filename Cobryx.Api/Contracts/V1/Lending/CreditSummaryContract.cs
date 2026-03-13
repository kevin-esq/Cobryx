namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Professional summary of a credit line facility.
/// </summary>
public record CreditSummaryContract(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    decimal PrincipalAmount,
    string Currency,
    decimal InterestRate,
    int InstallmentsCount,
    string Status,
    DateTime StartDate,
    decimal TotalPaid,
    decimal RemainingBalance
)
{
    /// <summary>Unique identifier for the credit facility.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Identifier of the associated customer.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public Guid CustomerId { get; init; } = CustomerId;

    /// <summary>Display name of the customer.</summary>
    /// <example>Jane Smith</example>
    public string CustomerName { get; init; } = CustomerName;

    /// <summary>The original principal amount.</summary>
    /// <example>25000.00</example>
    public decimal PrincipalAmount { get; init; } = PrincipalAmount;

    /// <summary>Currency code (ISO 4217).</summary>
    /// <example>MXN</example>
    public string Currency { get; init; } = Currency;

    /// <summary>Applied interest rate.</summary>
    /// <example>0.12</example>
    public decimal InterestRate { get; init; } = InterestRate;

    /// <summary>Total number of installments.</summary>
    /// <example>24</example>
    public int InstallmentsCount { get; init; } = InstallmentsCount;

    /// <summary>Operational status (ACTIVE, CLOSED, DELINQUENT).</summary>
    /// <example>ACTIVE</example>
    public string Status { get; init; } = Status;

    /// <summary>The date the facility was activated.</summary>
    /// <example>2026-01-01T00:00:00Z</example>
    public DateTime StartDate { get; init; } = StartDate;

    /// <summary>Sum of all payments received.</summary>
    /// <example>5000.00</example>
    public decimal TotalPaid { get; init; } = TotalPaid;

    /// <summary>Current outstanding balance including interest/fees.</summary>
    /// <example>21500.00</example>
    public decimal RemainingBalance { get; init; } = RemainingBalance;
}
