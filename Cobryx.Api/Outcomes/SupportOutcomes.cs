using Cobryx.Domain.Shared;

namespace Cobryx.Api.Outcomes;

public static class SupportOutcomes
{
    private const string Prefix = "SUPPORT";

    public static readonly Outcome TicketCreated = new($"{Prefix}.TICKET.CREATE_SUCCESS", OutcomeCategory.Success, "Support ticket created successfully.");
    public static readonly Outcome TicketUpdated = new($"{Prefix}.TICKET.UPDATE_SUCCESS", OutcomeCategory.Success, "Support ticket updated successfully.");
    public static readonly Outcome SearchCompleted = new($"{Prefix}.TICKET.SEARCH_SUCCESS", OutcomeCategory.Success, "Support ticket search completed.");
}
