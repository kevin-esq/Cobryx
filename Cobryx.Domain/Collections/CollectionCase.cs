namespace Cobryx.Domain.Collections;

public class CollectionCase
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid LoanId { get; private set; }
    public CollectionStage Stage { get; private set; }
    public int DaysPastDue { get; private set; }
    public decimal Outstanding { get; private set; }
    public Guid? AssignedAgentId { get; private set; }
    public DateTime? LastContactedAt { get; private set; }
    public DateTime? NextActionAt { get; private set; }
    public bool IsClosed { get; private set; }
    public int PriorityScore { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private CollectionCase() { }

    public CollectionCase(Guid tenantId, Guid loanId, int daysPastDue, decimal outstanding)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        LoanId = loanId;
        DaysPastDue = daysPastDue;
        Outstanding = outstanding;
        Stage = CollectionStage.Current;
        IsClosed = false;
        PriorityScore = 0;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void ApplyDecision(CollectionStage newStage, int priorityScore, DateTime? nextActionAt = null)
    {
        Stage = newStage;
        PriorityScore = priorityScore;
        NextActionAt = nextActionAt ?? NextActionAt;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SyncState(int daysPastDue, decimal outstanding)
    {
        DaysPastDue = daysPastDue;
        Outstanding = outstanding;
        UpdatedAt = DateTime.UtcNow;

        if (daysPastDue == 0 && outstanding == 0)
        {
            IsClosed = true;
            Stage = CollectionStage.Current;
            PriorityScore = 0;
        }
    }

    public void AssignAgent(Guid agentId)
    {
        AssignedAgentId = agentId;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkContacted(DateTime contactTime)
    {
        LastContactedAt = contactTime;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Close()
    {
        IsClosed = true;
        PriorityScore = 0;
        UpdatedAt = DateTime.UtcNow;
    }
}
