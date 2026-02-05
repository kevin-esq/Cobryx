using Cobryx.Domain.Common;

namespace Cobryx.Domain.Exceptions.Tenants;

public class TenantNotFoundException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Tenant.NotFound;

    public TenantNotFoundException(Guid tenantId)
        : base()
    {
        Metadata.Add("TenantId", tenantId);
    }
}

public class TenantContextMissingException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Tenant.ContextMissing;

    public TenantContextMissingException()
        : base()
    {
    }
}

public class OnboardingCompletedException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Tenant.OnboardingCompleted;
    public OnboardingCompletedException() : base() { }
}

public class OnboardingRequiredException : CobryxException
{
    public override string ErrorCode => DomainErrorCodes.Tenant.OnboardingRequired;
    public OnboardingRequiredException() : base() { }
}
