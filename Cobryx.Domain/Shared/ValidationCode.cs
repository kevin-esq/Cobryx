namespace Cobryx.Domain.Shared;

/// <summary>
/// Machine-readable code for field-level validation errors.
/// Message is internal-only (logs/debugging) — never serialized to the client.
/// </summary>
/// <param name="Code">The unique code (e.g., REQUIRED, INVALID_FORMAT).</param>
/// <param name="Message">Internal-only description for logging and debugging.</param>
/// <param name="Category">The category (defaults to BusinessError).</param>
public record ValidationCode(string Code, string? Message = null, OutcomeCategory Category = OutcomeCategory.BusinessError)
{
    public override string ToString() => Code;

    public static implicit operator string(ValidationCode validationCode) => validationCode.Code;

    public static ValidationCode FromExternal(string code) => new(code);
}
