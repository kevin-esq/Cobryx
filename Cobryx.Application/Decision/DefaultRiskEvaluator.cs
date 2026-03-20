using Cobryx.Application.Common.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

namespace Cobryx.Application.Decision;

public class DefaultRiskEvaluator(
    ML.MlClient mlClient,
    ML.ModelRouter router,
    ML.EnsembleService ensemble,
    ICobryxDbContext db) : IRiskEvaluator
{
    public async Task<(decimal pd, string modelVersion)> EvaluateRiskAsync(Guid customerId, DecisionContext ctx,
        FeatureVector features)
    {
        var heuristicPd = ctx.Credit.ProbabilityOfDefault;
        decimal prodPd;
        string prodVersion;

        try
        {
            (prodPd, prodVersion) = await mlClient.PredictAsync(features, "xgb_v1");
        }
        catch
        {
            prodPd = heuristicPd;
            prodVersion = "fallback-heuristic";
        }

        decimal shadowPd = prodPd;

        if (router.ShouldRunShadow())
        {
            try
            {
                (shadowPd, var shadowVersion) = await mlClient.PredictAsync(features, "xgb_v2");

                db.ShadowPredictions.Add(new ShadowPrediction
                {
                    CustomerId = customerId,
                    ProductionPd = prodPd,
                    ShadowPd = shadowPd,
                    ProductionModelVersion = prodVersion,
                    ShadowModelVersion = shadowVersion,
                    CreatedAt = DateTime.UtcNow
                });
            }
            catch (Exception)
            {
                /* ignored */
            }
        }

        var finalPd = ensemble.Combine(heuristicPd, prodPd, shadowPd);
        return (finalPd, prodVersion);
    }
}
