namespace Cobryx.Application.ML;

public class EnsembleService
{
    public decimal Combine(decimal heuristicPd, decimal mlPd, decimal shadowPd)
    {
        return
            (heuristicPd * 0.2m) +
            (mlPd * 0.6m) +
            (shadowPd * 0.2m);
    }
}
