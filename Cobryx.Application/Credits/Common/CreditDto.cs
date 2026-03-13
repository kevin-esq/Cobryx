using Cobryx.Domain.Lending.Enums;


namespace Cobryx.Application.Credits.Common;

public record CreditDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    decimal PrincipalAmount,
    string Currency,
    decimal InterestRate,
    int InstallmentsCount,
    CreditStatus Status,
    DateTime StartDate,
    decimal TotalPaid,
    decimal RemainingBalance);
