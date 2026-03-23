namespace Cobryx.Application.Collections.Models;

public class PaymentBehaviorProfile
{
    public int MissedPayments { get; set; }
    public bool HasPartialPayments { get; set; }
    public decimal PaymentConsistencyScore { get; set; }
}
