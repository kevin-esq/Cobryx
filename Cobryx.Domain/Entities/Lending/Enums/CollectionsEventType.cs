namespace Cobryx.Domain.Entities.Lending.Enums;

public enum CollectionsEventType
{
    EnteredArrears,
    StageEscalated,
    StageDeescalated, // For when they pay and improve status
    LateFeeApplied,
    DefaultTriggered,
    WriteOffExecuted,
    CollectionsManualNote
}
