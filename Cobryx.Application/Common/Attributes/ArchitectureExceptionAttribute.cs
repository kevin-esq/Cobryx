namespace Cobryx.Application.Common.Attributes;

/// <summary>
/// Marks a type as having an explicit architecture exception.
/// All exceptions must be documented with a reason.
/// Architecture tests will verify that all exceptions are explicitly marked.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ArchitectureExceptionAttribute : Attribute
{
    /// <summary>
    /// The reason why this type is exempt from architecture rules.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// The specific rule being exempted.
    /// </summary>
    public string? Rule { get; init; }

    /// <summary>
    /// Who approved this exception.
    /// </summary>
    public string? ApprovedBy { get; init; }
}
