using Cobryx.Domain.Enums;

namespace Cobryx.Api.Contracts.V1.Lending;

/// <summary>
/// Public contract for creating a credit line facility.
/// </summary>
/// <remarks>
/// A **Credit** represents a revolving line of credit. Unlike a Loan (which is a fixed amortized agreement),
/// a credit facility defines the maximum borrowing capacity and terms under which draws may occur.
/// </remarks>
public record CreateCreditRequest(
    Guid CustomerId,
    decimal Amount,
    string Currency,
    decimal InterestRate,
    InterestType InterestType,
    PaymentFrequency Frequency,
    int InstallmentsCount,
    int GraceDays = 0,
    Guid? ProductId = null
);
