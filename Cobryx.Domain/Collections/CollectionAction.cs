namespace Cobryx.Domain.Collections;

public class CollectionAction
{
    public Guid Id { get; private set; }
    public Guid CaseId { get; private set; }
    public CollectionActionType ActionType { get; private set; }
    public string? Notes { get; private set; }
    public DateTime ExecutedAt { get; private set; }

    private CollectionAction() { }

    public CollectionAction(Guid caseId, CollectionActionType actionType, string notes)
    {
        Id = Guid.NewGuid();
        CaseId = caseId;
        ActionType = actionType;
        Notes = notes;
        ExecutedAt = DateTime.UtcNow;
    }
}
