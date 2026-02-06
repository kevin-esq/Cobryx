using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Credits;

public class CreditNotFoundException : CobryxException
{
    public override string ErrorCode => "CREDITS.NOT_FOUND";

    public CreditNotFoundException(Guid creditId)
        : base(null!)
    {
        Metadata.Add("CreditId", creditId);
    }
}

public class InsufficientCreditsException : CobryxException
{
    public override string ErrorCode => "CREDITS.INSUFFICIENT";

    public InsufficientCreditsException(decimal currentBalance, decimal requiredAmount)
        : base(null!)
    {
        Metadata.Add("CurrentBalance", currentBalance);
        Metadata.Add("RequiredAmount", requiredAmount);
    }
}
