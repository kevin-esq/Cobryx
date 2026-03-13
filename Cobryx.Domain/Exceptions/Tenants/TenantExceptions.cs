using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Exceptions.Tenants;

public class TenantNotFoundException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Tenant.NotFound;

    public TenantNotFoundException(Guid tenantId)
        : base()
    {
        Metadata.Add("TenantId", tenantId);
    }
}

public class TenantContextMissingException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Tenant.ContextMissing;

    public TenantContextMissingException()
        : base()
    {
    }
}

public class OnboardingCompletedException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Tenant.OnboardingCompleted;
    public OnboardingCompletedException() : base() { }
}

public class OnboardingRequiredException : CobryxException
{
    public override DomainErrorCode ErrorCode => DomainErrorCode.Tenant.OnboardingRequired;
    public OnboardingRequiredException() : base() { }
}
