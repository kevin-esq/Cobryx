namespace Cobryx.Application.Common.Interfaces;

public interface ITenantProvider
{
    Guid? GetTenantId();
    void SetTenantId(Guid tenantId);
}
