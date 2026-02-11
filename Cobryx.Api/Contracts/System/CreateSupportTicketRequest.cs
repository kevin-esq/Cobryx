using Cobryx.Domain.Entities;

namespace Cobryx.Api.Contracts.V1.System;

/// <summary>
/// Public contract for creating a support or technical ticket.
/// </summary>
public record CreateSupportTicketRequest(
    string Subject,
    string Description,
    SupportTicketPriority Priority = SupportTicketPriority.Medium,
    string? Category = null
);
