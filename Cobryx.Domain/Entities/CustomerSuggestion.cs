using Cobryx.Domain.Common;

namespace Cobryx.Domain.Entities;

public class CustomerSuggestion : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public string? Status { get; private set; }
    public int Votes { get; private set; }

    private CustomerSuggestion()
    {
        Title = null!;
        Description = null!;
    }

    public CustomerSuggestion(Guid tenantId, Guid userId, string title, string description)
    {
        TenantId = tenantId;
        UserId = userId;
        Title = title;
        Description = description;
        Status = "Pending";
        Votes = 0;
    }

    public void UpdateStatus(string status)
    {
        Status = status;
        UpdateTimestamp();
    }

    public void Vote()
    {
        Votes++;
        UpdateTimestamp();
    }
}
