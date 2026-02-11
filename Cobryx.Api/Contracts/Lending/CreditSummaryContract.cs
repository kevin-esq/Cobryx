using System;

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
);
