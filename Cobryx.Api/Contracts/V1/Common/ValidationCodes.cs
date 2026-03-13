using Cobryx.Domain.Shared;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// Centralized machine-readable codes for field-level validation errors.
/// </summary>
public static class ValidationCodes
{
    /// <summary>Required field is missing or empty.</summary>
    public static readonly ValidationCode Required = new("REQUIRED", "Field is required.");

    /// <summary>Request body is empty when data was expected.</summary>
    public static readonly ValidationCode EmptyBody = new("EMPTY_BODY", "Request body is empty.");

    /// <summary>Payload structure or content is invalid (e.g. malformed JSON or binary data).</summary>
    public static readonly ValidationCode InvalidPayload = new("INVALID_PAYLOAD", "Payload is invalid.");

    /// <summary>Operation failed due to a domain-specific constraint violation.</summary>
    public static readonly ValidationCode DomainError = new("DOMAIN_ERROR", "Domain error occurred.");

    /// <summary>A unique constraint was violated (e.g. duplicate resource).</summary>
    public static readonly ValidationCode Duplicate = new("DUPLICATE", "Duplicate resource found.");
}
