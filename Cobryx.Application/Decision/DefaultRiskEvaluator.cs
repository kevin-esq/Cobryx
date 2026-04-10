using Cobryx.Application.Common.Interfaces;
using Cobryx.Application.ML;
using Cobryx.Application.ML.Interfaces;
using Cobryx.Domain.Decision;
using Cobryx.Domain.ML;

namespace Cobryx.Application.Decision
{
    public class DefaultRiskEvaluator(
        IMlClient mlClient,
        IModelRouter router,
        EnsembleService ensemble,
        ICobryxDbContext db,
        IClock clock,
        IFeatureFlags featureFlags) : IRiskEvaluator
    {
        public async Task<(decimal pd, string modelVersion)> EvaluateRiskAsync(Guid customerId, DecisionContext ctx,
            FeatureVector features)
        {
            var heuristicPd = ctx.Credit.ProbabilityOfDefault;
            decimal prodPd;
            string prodVersion;

            // ML Kill Switch - use heuristic rules when ML is disabled
            if (!featureFlags.IsMlScoringEnabled)
            {
                return (heuristicPd, "kill-switch-heuristic");
            }

            try
            {
                (prodPd, prodVersion) = await mlClient.PredictAsync(features, "xgb_v1");
            }
            catch
            {
                prodPd = heuristicPd;
                prodVersion = "fallback-heuristic";
            }

            var shadowPd = prodPd;

            if (router.ShouldRunShadow())
            {
                try
                {
                    (shadowPd, var shadowVersion) = await mlClient.PredictAsync(features, "xgb_v2");

                    _ = db.ShadowPredictions.Add(new ShadowPrediction(
                        customerId,
                        prodPd,
                        shadowPd,
                        prodVersion,
                        shadowVersion,
                        clock.UtcNow));
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
}
