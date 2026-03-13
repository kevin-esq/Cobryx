using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Exceptions;

public class SubscriptionLimitExceededException(DomainErrorCode errorCode, Dictionary<string, object>? metadata = null) : CobryxException(metadata)
{
    private readonly DomainErrorCode _errorCode = errorCode;
    public override DomainErrorCode ErrorCode => _errorCode;

    public static SubscriptionLimitExceededException LimitReached(string resource)
    {
        _ = resource;
        return new(DomainErrorCode.Subscription.LimitReached);
    }

    public static SubscriptionLimitExceededException Expired() =>
        new(DomainErrorCode.Subscription.Expired);

    public static SubscriptionLimitExceededException Blocked() =>
        new(DomainErrorCode.Subscription.Blocked);
}
