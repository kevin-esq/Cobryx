using Cobryx.Domain.Shared;

namespace Cobryx.Domain.Decision
{
    /// <summary>
    /// A high-fidelity record of a production decision for regression testing and auditing.
    /// Contains anonymized inputs and execution traces for deterministic replay.
    /// </summary>
    public class ProductionSnapshot : BaseEntity
    {
        /// <summary>
        /// SHA256 Hash of the real CustomerId to ensure PII is never stored in snapshots.
        /// </summary>
        public string HashedCustomerId { get; private set; } = string.Empty;

        public string EngineVersion { get; private set; } = string.Empty;
        public string ConfigHash { get; private set; } = string.Empty;

        /// <summary>
        /// SHA256 Hash of the ExecutionTraceJson for verification and indexing.
        /// Generated at decision time.
        /// </summary>
        public string TraceHash { get; private set; } = string.Empty;

        public bool IsFullSnapshot { get; private set; }

        public string? FeatureVectorJson { get; private set; }
        public string? MacroStateJson { get; private set; }
        public string? PortfolioStateJson { get; private set; }
        public string? ExecutionTraceJson { get; private set; }
        public string? DecisionResultJson { get; private set; }

        public decimal ResultCreditLimit { get; private set; }
        public decimal ResultInterestRate { get; private set; }

        private ProductionSnapshot() { }

        public ProductionSnapshot(
            string hashedCustomerId,
            string engineVersion,
            string configHash,
            string traceHash,
            bool isFull,
            decimal limit,
            decimal rate,
            string? features = null,
            string? macro = null,
            string? portfolio = null,
            string? trace = null,
            string? result = null)
        {
            HashedCustomerId = hashedCustomerId;
            EngineVersion = engineVersion;
            ConfigHash = configHash;
            TraceHash = traceHash;
            IsFullSnapshot = isFull;
            ResultCreditLimit = limit;
            ResultInterestRate = rate;

            if (isFull)
            {
                FeatureVectorJson = features;
                MacroStateJson = macro;
                PortfolioStateJson = portfolio;
                ExecutionTraceJson = trace;
                DecisionResultJson = result;
            }
        }
    }
}
