namespace Cobryx.Domain.Collections;

public class CollectionOutcome
{
    public System.Guid Id { get; set; }
    public System.Guid TenantId { get; set; }
    public System.Guid ActionId { get; set; }
    public CollectionActionType ActionType { get; set; }
    public bool WasSuccessful { get; set; }
    public decimal AmountRecovered { get; set; }
    public int DaysToRecover { get; set; }
    public int DaysPastDueAtAction { get; set; }
}
