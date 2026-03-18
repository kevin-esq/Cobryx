namespace Cobryx.Application.ML;

public class RlPolicy
{
    public Cobryx.Domain.ML.DecisionAction Select(List<(Cobryx.Domain.ML.DecisionAction action, decimal value)> qValues)
    {
        if (!qValues.Any())
            return Cobryx.Domain.ML.DecisionAction.MediumRisk;

        if (Random.Shared.NextDouble() < 0.1)
            return qValues[Random.Shared.Next(qValues.Count)].action;

        return qValues.OrderByDescending(x => x.value).First().action;
    }
}
