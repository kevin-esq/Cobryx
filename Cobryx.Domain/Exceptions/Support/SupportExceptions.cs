using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Support;

public class SupportTicketNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Support.TicketNotFound;

    public SupportTicketNotFoundException(Guid ticketId)
        : base(null!)
    {
        Metadata.Add("TicketId", ticketId);
    }
}
