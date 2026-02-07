using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Credits;

public class CreditNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Credits.GenericNotFound;

    public CreditNotFoundException(Guid creditId)
        : base(null!)
    {
        Metadata.Add("CreditId", creditId);
    }
}

public class InsufficientCreditsException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Credits.Insufficient;

    public InsufficientCreditsException(decimal currentBalance, decimal requiredAmount)
        : base(null!)
    {
        Metadata.Add("CurrentBalance", currentBalance);
        Metadata.Add("RequiredAmount", requiredAmount);
    }
}
