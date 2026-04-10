using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

namespace Cobryx.Application.Decision.Models
{
    public interface IReplayInput
    {
        public Guid CustomerId { get; }
        public string ConfigHash { get; }
        public string EngineVersion { get; }
        public string? FeatureVectorJson { get; }
        public string? MacroStateJson { get; }
        public string? PortfolioStateJson { get; }
        public string? DecisionContextJson { get; }
        public string? ExecutionTraceJson { get; }
        public decimal OriginalLimit { get; }
        public decimal OriginalRate { get; }
        public int RandomSeed { get; }

        public Guid SourceId { get; }
        public bool IsProductionSnapshot { get; }
    }

    public class ReplaySnapshotAdapter(ReplaySnapshot snapshot) : IReplayInput
    {
        public Guid CustomerId => snapshot.CustomerId;
        public string ConfigHash => snapshot.ConfigHash;
        public string EngineVersion => snapshot.EngineVersion;
        public string FeatureVectorJson => snapshot.FeatureVectorJson;
        public string MacroStateJson => snapshot.MacroStateJson;
        public string PortfolioStateJson => snapshot.PortfolioStateJson;
        public string DecisionContextJson => snapshot.DecisionContextJson;
        public string ExecutionTraceJson => snapshot.ExecutionTraceJson;
        public decimal OriginalLimit => snapshot.OriginalCreditLimit;
        public decimal OriginalRate => snapshot.OriginalInterestRate;
        public int RandomSeed => snapshot.RandomSeed;
        public Guid SourceId => snapshot.Id;
        public bool IsProductionSnapshot => false;
    }

    public class ProductionSnapshotAdapter(ProductionSnapshot snapshot) : IReplayInput
    {
        public Guid CustomerId => Guid.Empty;
        public string ConfigHash => snapshot.ConfigHash;
        public string EngineVersion => snapshot.EngineVersion;
        public string? FeatureVectorJson => snapshot.FeatureVectorJson;
        public string? MacroStateJson => snapshot.MacroStateJson;
        public string? PortfolioStateJson => snapshot.PortfolioStateJson;
        public string? DecisionContextJson => snapshot.DecisionResultJson;
        public string? ExecutionTraceJson => snapshot.ExecutionTraceJson;
        public decimal OriginalLimit => snapshot.ResultCreditLimit;
        public decimal OriginalRate => snapshot.ResultInterestRate;
        public int RandomSeed => 0;
        public Guid SourceId => snapshot.Id;
        public bool IsProductionSnapshot => true;
    }
}
