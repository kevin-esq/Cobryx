using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Decision
{
    public class ShadowDriftEvent : BaseEntity
    {
        public Guid SnapshotId { get; private set; }
        public string EngineVersion { get; private set; } = string.Empty;
        public string ShadowVersion { get; private set; } = string.Empty;

        public decimal DeltaLimit { get; private set; }
        public decimal DeltaRate { get; private set; }

        public DriftSeverity OutputSeverity { get; private set; }
        public DriftSeverity TraceSeverity { get; private set; }

        public string? AttributionJson { get; private set; }
        public DateTime OccurredAt { get; private set; } = DateTime.UtcNow;

        // ReSharper disable once UnusedMember.Local — Required by EF Core
        private ShadowDriftEvent() { }

        public ShadowDriftEvent(
            Guid snapshotId,
            string engineVersion,
            string shadowVersion,
            decimal deltaLimit,
            decimal deltaRate,
            DriftSeverity outputSeverity,
            DriftSeverity traceSeverity,
            string? attributionJson)
        {
            SnapshotId = snapshotId;
            EngineVersion = engineVersion;
            ShadowVersion = shadowVersion;
            DeltaLimit = deltaLimit;
            DeltaRate = deltaRate;
            OutputSeverity = outputSeverity;
            TraceSeverity = traceSeverity;
            AttributionJson = attributionJson;
            OccurredAt = DateTime.UtcNow;
        }
    }
}
