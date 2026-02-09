using Cobryx.Domain.Entities.Lending.Enums;

namespace Cobryx.Application.Lending.Dtos;

public record LoanScheduleDto(
    Guid LoanId,
    string LoanNumber,
    LoanStatus Status,
    decimal TotalPrincipal,
    decimal TotalInterest,
    decimal TotalPaid,
    IReadOnlyList<InstallmentDto> Installments
);

public record InstallmentDto(
    Guid Id,
    int InstallmentNumber,
    DateTime DueDate,
    decimal PrincipalAmount,
    decimal InterestAmount,
    decimal TotalAmount,
    decimal PrincipalPaid,
    decimal InterestPaid,
    decimal LateFeesPaid,
    decimal TotalPaid,
    decimal RemainingAmount,
    InstallmentStatus Status,
    DateTime? PaidAt
);
