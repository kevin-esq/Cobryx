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

    public CollectionCase(Guid tenantId, Guid loanId, int daysPastDue, decimal outstanding, DateTime? now = null)
    {
        var timestamp = now ?? DateTime.UtcNow;
        Id = Guid.NewGuid();
        TenantId = tenantId;
        LoanId = loanId;
        DaysPastDue = daysPastDue;
        Outstanding = outstanding;
        Stage = CollectionStage.Current;
        IsClosed = false;
        PriorityScore = 0;
        CreatedAt = timestamp;
        UpdatedAt = timestamp;
    }

    public void ApplyDecision(CollectionStage newStage, int priorityScore, DateTime? nextActionAt = null)
        => ApplyDecision(newStage, priorityScore, DateTime.UtcNow, nextActionAt);

    public void ApplyDecision(CollectionStage newStage, int priorityScore, DateTime now, DateTime? nextActionAt = null)
    {
        Stage = newStage;
        PriorityScore = priorityScore;
        NextActionAt = nextActionAt ?? NextActionAt;
        UpdatedAt = now;
    }

    public void SyncState(int daysPastDue, decimal outstanding) => SyncState(daysPastDue, outstanding, DateTime.UtcNow);

    public void SyncState(int daysPastDue, decimal outstanding, DateTime now)
    {
        DaysPastDue = daysPastDue;
        Outstanding = outstanding;
        UpdatedAt = now;

        if (daysPastDue == 0 && outstanding == 0)
        {
            IsClosed = true;
            Stage = CollectionStage.Current;
            PriorityScore = 0;
        }
    }

    public void AssignAgent(Guid agentId) => AssignAgent(agentId, DateTime.UtcNow);

    public void AssignAgent(Guid agentId, DateTime now)
    {
        AssignedAgentId = agentId;
        UpdatedAt = now;
    }

    public void MarkContacted(DateTime contactTime) => MarkContacted(contactTime, DateTime.UtcNow);

    public void MarkContacted(DateTime contactTime, DateTime now)
    {
        LastContactedAt = contactTime;
        UpdatedAt = now;
    }

    public void Close() => Close(DateTime.UtcNow);

    public void Close(DateTime now)
    {
        IsClosed = true;
        PriorityScore = 0;
        UpdatedAt = now;
    }
}
