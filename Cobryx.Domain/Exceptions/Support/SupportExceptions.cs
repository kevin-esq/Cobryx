using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Support;

public class SupportTicketNotFoundException : CobryxException
{
    public override string ErrorCode => "SUPPORT.TICKET_NOT_FOUND";

    public SupportTicketNotFoundException(Guid ticketId)
        : base($"Support ticket with ID {ticketId} was not found.")
    {
        Metadata.Add("TicketId", ticketId);
    }
}
