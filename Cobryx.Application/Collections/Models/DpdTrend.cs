namespace Cobryx.Application.Collections.Models;

public class DpdTrend
{
    public int CurrentDpd { get; set; }
    public int PreviousDpd { get; set; }

    public int Delta => CurrentDpd - PreviousDpd;
}
