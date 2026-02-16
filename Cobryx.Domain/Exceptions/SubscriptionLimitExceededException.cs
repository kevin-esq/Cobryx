using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions;

public class SubscriptionLimitExceededException : CobryxException
{
    private readonly DomainErrorCode _errorCode;
    public override DomainErrorCode ErrorCode => _errorCode;

    public SubscriptionLimitExceededException(DomainErrorCode errorCode, Dictionary<string, object>? metadata = null)
        : base(metadata)
    {
        _errorCode = errorCode;
    }

    public static SubscriptionLimitExceededException LimitReached(string resource) =>
        new(DomainErrorCode.Subscription.LimitReached);

    public static SubscriptionLimitExceededException Expired() =>
        new(DomainErrorCode.Subscription.Expired);

    public static SubscriptionLimitExceededException Blocked() =>
        new(DomainErrorCode.Subscription.Blocked);
}
