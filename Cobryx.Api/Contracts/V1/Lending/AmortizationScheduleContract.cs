namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Professional amortization schedule providing a detailed breakdown of loan repayment.
/// </summary>
public record AmortizationScheduleContract(
    Guid LoanId,
    string LoanNumber,
    string Status,
    decimal OriginalPrincipal,
    decimal TotalInterest,
    decimal TotalPaid,
    List<AmortizationInstallmentContract> Installments
)
{
    /// <summary>Unique identifier for the parent loan.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid LoanId { get; init; } = LoanId;

    /// <summary>User-friendly loan reference number.</summary>
    /// <example>LN-2026-001</example>
    public string LoanNumber { get; init; } = LoanNumber;

    /// <summary>Aggregated status of the loan lifecycle (ACTIVE, CLOSED, DELINQUENT).</summary>
    /// <example>ACTIVE</example>
    public string Status { get; init; } = Status;

    /// <summary>The initial amount borrowed.</summary>
    /// <example>50000.00</example>
    public decimal OriginalPrincipal { get; init; } = OriginalPrincipal;

    /// <summary>Aggregated total interest projected over the loan term.</summary>
    /// <example>4500.00</example>
    public decimal TotalInterest { get; init; } = TotalInterest;

    /// <summary>Total amount paid to date across all installments.</summary>
    /// <example>15000.00</example>
    public decimal TotalPaid { get; init; } = TotalPaid;

    /// <summary>Detailed list of period-by-period obligations.</summary>
    public List<AmortizationInstallmentContract> Installments { get; init; } = Installments;
}

/// <summary>
/// Individual installment details within an amortization schedule, including payment tracking.
/// </summary>
public record AmortizationInstallmentContract(
    Guid Id,
    int Number,
    DateTime DueDate,
    decimal Principal,
    decimal Interest,
    decimal TotalDue,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal LateFeesPaid,
    decimal TotalPaid,
    decimal RemainingBalance,
    string Status,
    DateTime? PaidAt = null
)
{
    /// <summary>Unique record identifier for the installment.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa7</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Sequential installment number.</summary>
    /// <example>1</example>
    public int Number { get; init; } = Number;

    /// <summary>The date when payment is expected.</summary>
    /// <example>2026-03-01T00:00:00Z</example>
    public DateTime DueDate { get; init; } = DueDate;

    /// <summary>Portion of the payment applied to principal.</summary>
    /// <example>2083.33</example>
    public decimal Principal { get; init; } = Principal;

    /// <summary>Portion of the payment applied to interest.</summary>
    /// <example>416.67</example>
    public decimal Interest { get; init; } = Interest;

    /// <summary>Total amount due for this installment (Principal + Interest).</summary>
    /// <example>2500.00</example>
    public decimal TotalDue { get; init; } = TotalDue;

    /// <summary>Aggregated principal amount paid for this period.</summary>
    /// <example>1500.00</example>
    public decimal PrincipalPaid { get; init; } = PrincipalPaid;

    /// <summary>Aggregated interest amount paid for this period.</summary>
    /// <example>416.67</example>
    public decimal InterestPaid { get; init; } = InterestPaid;

    /// <summary>Aggregated late fee penalties paid for this period.</summary>
    /// <example>50.00</example>
    public decimal LateFeesPaid { get; init; } = LateFeesPaid;

    /// <summary>Total amount collected for this installment.</summary>
    /// <example>1966.67</example>
    public decimal TotalPaid { get; init; } = TotalPaid;

    /// <summary>Outstanding amount for this specific installment.</summary>
    /// <example>583.33</example>
    public decimal RemainingBalance { get; init; } = RemainingBalance;

    /// <summary>Installment-level lifecycle status (PENDING, PARTIAL, PAID, OVERDUE).</summary>
    /// <example>PARTIAL</example>
    public string Status { get; init; } = Status;

    /// <summary>The date when the last payment was received for this installment.</summary>
    /// <example>2026-03-05T12:00:00Z</example>
    public DateTime? PaidAt { get; init; } = PaidAt;
}
