namespace Cobryx.Domain.ML.Simulation;

public class SimulatedCustomer
{
    public Guid Id { get; set; }

    public decimal CreditScore { get; set; }
    public decimal Income { get; set; }
    public decimal Utilization { get; set; }

    public bool IsDefaulted { get; set; }
    public decimal Outstanding { get; set; }
    public int DaysPastDue { get; set; }

    public decimal RiskTolerance { get; set; }
}
