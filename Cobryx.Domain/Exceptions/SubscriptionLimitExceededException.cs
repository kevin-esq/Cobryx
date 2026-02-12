using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions;

public class SubscriptionLimitExceededException : CobryxException
{
    private readonly DomainErrorCode _errorCode;
    public override DomainErrorCode ErrorCode => _errorCode;

    public SubscriptionLimitExceededException(DomainErrorCode errorCode, string message, Dictionary<string, object>? metadata = null) 
        : base(metadata)
    {
        _errorCode = errorCode;
    }

    public static SubscriptionLimitExceededException LimitReached(string resource) =>
        new(DomainErrorCode.Subscription.LimitReached, $"The limit for {resource} has been reached for your current plan.");

    public static SubscriptionLimitExceededException Expired() =>
        new(DomainErrorCode.Subscription.Expired, "Your subscription has expired. Please renew to continue using the service.");

    public static SubscriptionLimitExceededException Blocked() =>
        new(DomainErrorCode.Subscription.Blocked, "Your subscription is currently blocked or terminated.");
}
