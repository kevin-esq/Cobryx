using Cobryx.Domain.Collections;

namespace Cobryx.Application.Collections.Models;

public class CollectionDecision
{
    public CollectionStage Stage { get; set; }
    public CollectionActionType Action { get; set; }
    public int PriorityScore { get; set; }
    public DateTime NextActionAt { get; set; }
}
