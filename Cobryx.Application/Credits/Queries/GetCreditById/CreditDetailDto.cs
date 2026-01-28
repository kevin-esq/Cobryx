using Cobryx.Domain.Enums;

namespace Cobryx.Application.Credits.Queries.GetCreditById;

public record CreditDetailDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    decimal PrincipalAmount,
    string Currency,
    decimal InterestRate,
    int InstallmentsCount,
    CreditStatus Status,
    DateTime StartDate,
    IEnumerable<InstallmentDto> Schedule,
    IEnumerable<CreditPaymentDto> Payments);

public record InstallmentDto(
    int Number,
    DateTime DueDate,
    decimal TotalDue,
    decimal InterestDue,
    decimal PrincipalDue,
    decimal TotalPaid,
    InstallmentStatus Status);

public record CreditPaymentDto(
    Guid Id,
    decimal Amount,
    string Currency,
    DateTime PaymentDate,
    string? Reference);
