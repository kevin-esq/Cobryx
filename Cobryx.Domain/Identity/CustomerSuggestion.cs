using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Identity;

public enum SuggestionStatus { Pending, UnderReview, Accepted, Rejected, Implemented }

public class CustomerSuggestion : BaseEntity, ITenantEntity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public SuggestionStatus Status { get; private set; }
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
        Status = SuggestionStatus.Pending;
        Votes = 0;
    }

    public void UpdateStatus(SuggestionStatus status)
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
