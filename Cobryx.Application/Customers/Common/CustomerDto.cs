namespace Cobryx.Application.Customers.Common;

public record CustomerDto(
    Guid Id,
    string FullName,
    string Phone,
    string? Address,
    string? ExternalReference,
    int ActiveCreditsCount,
    DateTime CreatedAt);
