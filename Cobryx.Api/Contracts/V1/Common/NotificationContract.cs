using System.ComponentModel.DataAnnotations;

namespace Cobryx.Api.Contracts.V1.Common;

/// <summary>
/// A system or operational notification for a user/tenant.
/// </summary>
public record NotificationContract(
    [Required] Guid Id,
    [Required] string Title,
    [Required] string Message,
    string? Category,
    [Required] bool IsRead,
    [Required] DateTime CreatedAt
)
{
    /// <summary>Unique identifier for the notification.</summary>
    /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
    public Guid Id { get; init; } = Id;

    /// <summary>Short, descriptive title.</summary>
    /// <example>Payment Received</example>
    public string Title { get; init; } = Title;

    /// <summary>Detailed notification content.</summary>
    /// <example>Your payment of $1,500.00 for Invoice INV-2026-004 has been confirmed.</example>
    public string Message { get; init; } = Message;

    /// <summary>Functional category used for grouping/filtering.</summary>
    /// <remarks>Possible values: Billing, System, Security</remarks>
    /// <example>Billing</example>
    public string? Category { get; init; } = Category;

    /// <summary>Whether the user has acknowledged/read the notification.</summary>
    /// <example>false</example>
    public bool IsRead { get; init; } = IsRead;

    /// <summary>Precise timestamp of notification issuance (UTC).</summary>
    /// <example>2026-02-05T17:33:08Z</example>
    public DateTime CreatedAt { get; init; } = CreatedAt;
}
