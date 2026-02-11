using System;

namespace Cobryx.Api.Contracts.V1.Customers;

/// <summary>
/// Professional summary of a customer record.
/// </summary>
public record CustomerSummaryContract(
    Guid Id,
    string FirstName,
    string LastName,
    string FullName,
    string Phone,
    string? DocumentType,
    string? DocumentNumber,
    string? City,
    string? State,
    bool IsActive
);
