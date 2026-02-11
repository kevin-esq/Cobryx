using Cobryx.Domain.Enums;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Public-facing identity document structure.
/// </summary>
public record IdentityDocumentContract(
    DocumentType Type,
    string Number
);
