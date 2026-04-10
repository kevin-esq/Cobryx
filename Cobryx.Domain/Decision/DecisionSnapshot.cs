namespace Cobryx.Domain.Decision
{
    public class DecisionSnapshot
    {
        /// <summary>
        /// EF Core materialization constructor. Do not use directly.
        /// </summary>
        private DecisionSnapshot() { }

        public DecisionSnapshot(
            Guid customerId,
            decimal pd,
            string modelVersion,
            decimal limit,
            decimal rate,
            decimal fraudScore)
        {
            Id = Guid.NewGuid();
            CustomerId = customerId;
            ProbabilityOfDefault = pd;
            CreditLimit = limit;
            InterestRate = rate;
            FraudScore = fraudScore;
            ModelVersion = modelVersion;
            CreatedAt = DateTime.UtcNow;
        }

        public Guid Id { get; private set; }
        public Guid CustomerId { get; private set; }

        public decimal ProbabilityOfDefault { get; private set; }
        public decimal CreditLimit { get; private set; }
        public decimal InterestRate { get; private set; }
        public decimal FraudScore { get; private set; }
        public string ModelVersion { get; private set; } = string.Empty;

        public DateTime CreatedAt { get; private set; }
    }
}
