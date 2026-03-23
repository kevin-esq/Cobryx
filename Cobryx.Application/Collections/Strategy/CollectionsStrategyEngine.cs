using Cobryx.Application.Collections.Models;
using Cobryx.Domain.Collections;

namespace Cobryx.Application.Collections.Strategy;

public class CollectionsStrategyEngine : ICollectionsStrategyEngine
{
    public CollectionDecision Evaluate(
        int daysPastDue,
        decimal outstanding,
        CustomerRiskProfile risk,
        PaymentBehaviorProfile behavior,
        DpdTrend trend,
        Cobryx.Application.Collections.Optimizer.StrategyWeights weights)
    {
        var dpd = daysPastDue;
        var riskScore = risk.Score;

        var priorityScore = (int)((outstanding * 0.5m) + (dpd * 2) + (riskScore * 1.5m));

        if (Random.Shared.NextDouble() < 0.10)
        {
            var actions = new[] { CollectionActionType.SmsReminder, CollectionActionType.EmailReminder, CollectionActionType.AgentCall };
            return new CollectionDecision
            {
                Stage = CollectionStage.Contact,
                Action = actions[Random.Shared.Next(actions.Length)],
                PriorityScore = priorityScore,
                NextActionAt = DateTime.UtcNow.AddDays(1)
            };
        }

        var bestEarlyAction = weights.EmailWeight > weights.SmsWeight
            ? CollectionActionType.EmailReminder
            : CollectionActionType.SmsReminder;

        if (dpd <= 3)
        {
            return new CollectionDecision
            {
                Stage = CollectionStage.Reminder,
                Action = bestEarlyAction,
                PriorityScore = priorityScore,
                NextActionAt = System.DateTime.UtcNow.AddDays(1)
            };
        }

        if (dpd <= 7)
        {
            if (riskScore < 40 || trend.Delta > 5)
            {
                return new CollectionDecision
                {
                    Stage = CollectionStage.Contact,
                    Action = CollectionActionType.AgentCall,
                    PriorityScore = priorityScore,
                    NextActionAt = DateTime.UtcNow.AddDays(1)
                };
            }

            return new CollectionDecision
            {
                Stage = CollectionStage.Reminder,
                Action = CollectionActionType.EmailReminder,
                PriorityScore = priorityScore,
                NextActionAt = DateTime.UtcNow.AddDays(2)
            };
        }

        if (dpd <= 30)
        {
            var highExposure = outstanding > 10000;

            return new CollectionDecision
            {
                Stage = CollectionStage.Escalation,
                Action = highExposure
                    ? CollectionActionType.AgentCall
                    : CollectionActionType.PaymentPlanOffer,
                PriorityScore = priorityScore,
                NextActionAt = DateTime.UtcNow.AddDays(3)
            };
        }

        return new CollectionDecision
        {
            Stage = CollectionStage.Legal,
            Action = CollectionActionType.LegalNotice,
            PriorityScore = (int)(priorityScore * weights.LegalWeight),
            NextActionAt = System.DateTime.UtcNow.AddDays(7)
        };
    }
}
