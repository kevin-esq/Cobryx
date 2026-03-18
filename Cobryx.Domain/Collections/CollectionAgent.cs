namespace Cobryx.Domain.Collections;

public class CollectionAgent
{
    public System.Guid Id { get; set; }
    public System.Guid TenantId { get; set; }
    public int MaxCapacity { get; set; }
    public int CurrentLoad { get; set; }
    public bool IsActive { get; set; }
}
