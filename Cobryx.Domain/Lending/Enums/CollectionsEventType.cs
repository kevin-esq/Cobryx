namespace Cobryx.Domain.Lending.Enums;

public enum CollectionsEventType
{
    EnteredArrears,
    StageEscalated,
    StageDeescalated,
    LateFeeApplied,
    DefaultTriggered,
    WriteOffExecuted,
    CollectionsManualNote
}
