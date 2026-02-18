namespace Cobryx.Domain.Common;

/// <summary>
/// Strongly-typed outcome for API responses and application results.
/// </summary>
/// <param name="Value">Machine-readable outcome code.</param>
/// <param name="Description">Human-readable description.</param>
public enum OutcomeCategory
{
    Success,
    Info,
    Warning,
    BusinessError,
    Critical
}

/// <summary>
/// Strongly-typed outcome for API responses and application results.
/// </summary>
/// <param name="Value">Machine-readable outcome code.</param>
/// <param name="Category">Severity or type of the outcome.</param>
/// <param name="Description">Human-readable description (for logs and autodoc).</param>
public record Outcome(string Value, OutcomeCategory Category, string? Description = null)
{
    public override string ToString() => Value;

    public static implicit operator string(Outcome outcome) => outcome.Value;

    /// <summary>
    /// Creates an outcome from an external source.
    /// Use sparingly for codes not defined in our system.
    /// </summary>
    public static Outcome FromExternal(string value, OutcomeCategory category = OutcomeCategory.BusinessError, string? source = null) 
        => new(value, category, $"External code from {source ?? "unknown source"}");
}
