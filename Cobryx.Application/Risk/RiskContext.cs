namespace Cobryx.Application.Risk;

public class RiskContext
{
    public int DaysPastDue { get; set; }
    public decimal Utilization { get; set; }
    public decimal Outstanding { get; set; }
    public decimal CreditLimit { get; set; }
    public int PaymentDelayDays { get; set; }

    public decimal PreviousUtilization { get; set; }
    public int PreviousPaymentDelayDays { get; set; }
}
