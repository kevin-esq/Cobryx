using Cobryx.Application.Collections.Models;
using Cobryx.Domain.Analytics;
using Cobryx.Domain.Collections;

namespace Cobryx.Application.Collections.Strategy;

public class CollectionsStrategyEngine : ICollectionsStrategyEngine
{
    public CollectionDecision Evaluate(
        LoanBalanceSnapshot snapshot,
        CustomerRiskProfile risk,
        PaymentBehaviorProfile behavior,
        DpdTrend trend)
    {
        var dpd = snapshot.DaysPastDue;
        var riskScore = risk.Score;
        var outstanding = snapshot.Outstanding;

        var priorityScore = (int)((outstanding * 0.5m) + (dpd * 2) + (riskScore * 1.5m));

        // 1. Early stage (0–3)
        if (dpd <= 3)
        {
            return new CollectionDecision
            {
                Stage = CollectionStage.Reminder,
                Action = CollectionActionType.SmsReminder,
                PriorityScore = priorityScore,
                NextActionAt = DateTime.UtcNow.AddDays(1)
            };
        }

        // 2. Medium risk logic
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

        // 3. Escalation logic
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

        // 4. Legal
        return new CollectionDecision
        {
            Stage = CollectionStage.Legal,
            Action = CollectionActionType.LegalNotice,
            PriorityScore = priorityScore,
            NextActionAt = DateTime.UtcNow.AddDays(7)
        };
    }
}
