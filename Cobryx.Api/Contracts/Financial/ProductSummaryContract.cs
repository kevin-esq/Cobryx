using System;

namespace Cobryx.Api.Contracts.V1.Financial;

/// <summary>
/// Professional summary of a financial product or service.
/// </summary>
public record ProductSummaryContract(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency
);
