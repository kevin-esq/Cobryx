using Cobryx.Application.ML.Models;
using Cobryx.Domain.Decision;

namespace Cobryx.Application.Decision.Models
{
    public class ShadowConfig
    {
        public const string SectionName = "Shadow";
        public bool Enabled { get; set; }
        public string ShadowEngineVersion { get; set; } = string.Empty;
        public decimal SamplingRate { get; set; } = 0.1m;
    }

    public class ShadowExecutionResult
    {
        public Guid SnapshotId { get; set; } = Guid.NewGuid();
        public ReplayResult Primary { get; set; } = default!;
        public ReplayResult Shadow { get; set; } = default!;

        public decimal DeltaLimit => Shadow.DeltaCreditLimit - Primary.DeltaCreditLimit;
        public decimal DeltaRate => Shadow.DeltaInterestRate - Primary.DeltaInterestRate;

        public DriftSeverity OutputSeverity { get; set; }
        public DriftSeverity TraceSeverity { get; set; }

        public DriftAttribution? Attribution { get; set; }

        public bool IsCritical => OutputSeverity == DriftSeverity.Critical
                                  || TraceSeverity == DriftSeverity.Critical;
    }
}
