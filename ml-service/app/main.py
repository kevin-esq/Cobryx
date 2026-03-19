from fastapi import FastAPI, Query
from typing import Annotated
from app.schemas import FeatureVector, PredictionResponse
from app.model import joblib
import numpy as np

from app.ppo_model import ActorCritic, sample_action
ppo_model = ActorCritic()

app = FastAPI()

def load_safe(filename):
    try:
        return joblib.load(filename)
    except Exception:
        class Dummy:
            def predict_proba(self, _):
                return [[0.0, 0.45]]
        return Dummy()

models = {
    "xgb_v1": load_safe("model_v1.pkl"),
    "xgb_v2": load_safe("model_v2.pkl"),
}

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/predict", response_model=PredictionResponse)
def predict(features: FeatureVector, model: Annotated[str, Query()] = "xgb_v1"):
    pipeline = models.get(model, models["xgb_v1"])

    X = np.array([[
        features.utilization,
        features.paymentDelay,
        features.behaviorScore,
        features.dpdTrend,
        features.outstanding
    ]])

    prob = pipeline.predict_proba(X)[0][1]

    return PredictionResponse(
        probabilityOfDefault=float(max(0.0, min(1.0, prob))),
        modelVersion=model
    )

@app.post("/rl/ppo/portfolio")
def decide_portfolio(state: dict):
    return {
        "creditMultiplier": 1.0,
        "riskTolerance": 0.5,
        "liquidityBuffer": 0.1
    }

@app.post("/rl/ppo/combined")
def decide_combined(payload: dict):
    features = payload.get("features", {})
    global_state = payload.get("global_state", {})
    macro = payload.get("macro", {})

    import torch
    x = torch.tensor([[
        features["utilization"],
        features["paymentDelay"] / 30.0,
        features["behaviorScore"],
        features["dpdTrend"] / 30.0,
        features["outstanding"] / 20000.0,
        global_state.get("totalExposure", 0) / 10000000.0,
        global_state.get("availableLiquidity", 0) / 500000.0,

        # 17 Audit: Normalized Macro Features
        macro.get("interestRate", 0) / 0.2,
        macro.get("inflation", 0) / 0.2,
        macro.get("creditSpread", 0) / 0.1,
        macro.get("volatility", 0) / 0.5,

        # 17 Audit: Non-Markovian History
        macro.get("inflationTMinus1", 0) / 0.2,
        macro.get("inflationTMinus2", 0) / 0.2,
        macro.get("rateTrend", 0) / 0.05,

        macro.get("timeToMaturity", 12) / 36.0
    ]]).float()

    mean, value = ppo_model(x)
    _, action, log_prob = sample_action(mean, ppo_model.log_std)

    credit_mult = float(0.2 + (action[0][0] + 1) / 2 * (2.0 - 0.2))
    interest_adj = float(-0.1 + (action[0][1] + 1) / 2 * 0.4)

    return {
        "portfolio": {
            "creditMultiplier": 1.0,
            "riskTolerance": 0.5,
            "liquidityBuffer": 0.1
        },
        "local": {
            "creditMultiplier": credit_mult,
            "interestDelta": interest_adj,
            "logProb": float(log_prob.item()),
            "value": float(value.item())
        }
    }

@app.post("/rl/ppo/montecarlo")
def montecarlo(payload: dict):
    import torch

    scenarios = payload.get("scenarios", [])
    features = payload.get("features", {})
    global_state = payload.get("global_state", {})
    macro = payload.get("macro", {})

    if not scenarios:
        return {"creditMultipliers": [], "interestDeltas": [], "values": [], "logProbs": []}

    with torch.no_grad():
        X = torch.tensor([
            [
                features.get("utilization", 0),
                features.get("paymentDelay", 0) / 30.0,
                features.get("behaviorScore", 0),
                features.get("dpdTrend", 0) / 30.0,
                features.get("outstanding", 0) / 20000.0,
                global_state.get("totalExposure", 0) / 10000000.0,
                global_state.get("availableLiquidity", 0) / 500000.0,

                # 18 Audit: Stochastic Macro Parameters per Scenario
                s.get("interestRate", 0) / 0.2,
                s.get("inflation", 0) / 0.2,
                macro.get("creditSpread", 0) / 0.1,
                macro.get("volatility", 0) / 0.5,

                # Constant history for the moment of decision
                macro.get("inflationTMinus1", 0) / 0.2,
                macro.get("inflationTMinus2", 0) / 0.2,
                macro.get("rateTrend", 0) / 0.05,

                macro.get("timeToMaturity", 12) / 36.0
            ]
            for s in scenarios
        ]).float()

        mean, values = ppo_model(X)
        _, actions, log_probs = sample_action(mean, ppo_model.log_std)

        return {
            "creditMultipliers": (0.2 + (actions[:, 0] + 1) / 2 * (2.0 - 0.2)).tolist(),
            "interestDeltas": (-0.1 + (actions[:, 1] + 1) / 2 * 0.4).tolist(),
            "values": values.squeeze(-1).tolist(),
            "logProbs": log_probs.sum(dim=1).tolist()
        }
