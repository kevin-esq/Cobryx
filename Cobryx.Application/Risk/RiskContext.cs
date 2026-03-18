namespace Cobryx.Application.Risk;

public class RiskContext
{
    public int DaysPastDue { get; set; }
    public decimal Utilization { get; set; }
    public int PaymentDelayDays { get; set; }

    public decimal PreviousUtilization { get; set; }
    public int PreviousPaymentDelayDays { get; set; }
}
