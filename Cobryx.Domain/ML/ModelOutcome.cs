namespace Cobryx.Domain.ML
{
    public class ModelOutcome
    {
        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }

        // ReSharper disable once InconsistentNaming — EF Core column mapping: existing migrations reference this as PredictedPD
        public decimal PredictedPD { get; private set; }
        public string ModelVersion { get; private set; } = string.Empty;
        public bool Defaulted { get; private set; }
        public decimal AmountRecovered { get; private set; }

        public DateTime CreatedAt { get; private set; }

        // ReSharper disable once UnusedMember.Local — Required by EF Core
        private ModelOutcome() { }

        public ModelOutcome(Guid customerId, decimal pd, string modelVersion, bool defaulted, decimal recovered, DateTime? now = null)
        {
            Id = Guid.NewGuid();
            CustomerId = customerId;
            PredictedPD = pd;
            ModelVersion = modelVersion;
            Defaulted = defaulted;
            AmountRecovered = recovered;
            CreatedAt = now ?? DateTime.UtcNow;
        }
    }
}
