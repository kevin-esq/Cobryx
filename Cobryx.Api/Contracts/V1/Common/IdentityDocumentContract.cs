using Cobryx.Domain.Enums;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Public-facing identity document structure.
/// </summary>
public record IdentityDocumentContract(
    DocumentType Type,
    string Number
)
{
    /// <summary>The type of identity document (RFC, CURP, INE, PASSPORT).</summary>
    /// <example>CURP</example>
    public DocumentType Type { get; init; } = Type;

    /// <summary>The unique identifier value on the document.</summary>
    /// <example>SIMJ800101HDFLRS01</example>
    public string Number { get; init; } = Number;
}
