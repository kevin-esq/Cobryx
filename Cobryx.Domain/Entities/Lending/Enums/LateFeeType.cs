namespace Cobryx.Domain.Entities.Lending.Enums;

public enum LateFeeType
{
    Fixed = 1,           // One-time amount per overdue
    DailyFlat = 2,       // Flat amount per day
    DailyPercentage = 3  // % of balance per day
}
