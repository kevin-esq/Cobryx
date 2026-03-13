namespace Cobryx.Application.Common.Interfaces;

public interface ITenantProvider
{
    public Guid? GetTenantId();
    public void SetTenantId(Guid tenantId);
}
