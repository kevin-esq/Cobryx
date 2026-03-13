using System.ComponentModel.DataAnnotations;

using Cobryx.Domain.Identity;

namespace Cobryx.Api.Contracts.V1.System;

/// <summary>
/// Public contract for creating a support or technical ticket.
/// </summary>
public record CreateSupportTicketRequest(
    [Required] string Subject,
    [Required] string Description,
    SupportTicketPriority Priority = SupportTicketPriority.Medium,
    SupportTicketCategory Category = SupportTicketCategory.Inquiry
)
{
    /// <summary>Short summary of the issue.</summary>
    /// <example>Cannot access billing portal</example>
    public string Subject { get; init; } = Subject;

    /// <summary>Detailed description of the problem or request.</summary>
    /// <example>When I click on the billing tab, I get a 403 error. My user ID is user_123.</example>
    public string Description { get; init; } = Description;

    /// <summary>Relative importance of the ticket (Low, Medium, High, Critical).</summary>
    /// <example>High</example>
    public SupportTicketPriority Priority { get; init; } = Priority;

    /// <summary>Functional area of the ticket (e.g., Billing, Technical, Security).</summary>
    /// <example>Billing</example>
    public SupportTicketCategory Category { get; init; } = Category;
}
