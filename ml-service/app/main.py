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

@app.post("/rl/ppo/decide")
def decide_ppo(features: dict):
    import torch
    x = torch.tensor([[
        features["utilization"],
        features["paymentDelay"] / 30.0,
        features["behaviorScore"],
        features["dpdTrend"] / 30.0,
        features["outstanding"] / 20000.0
    ]]).float()

    mean, value = ppo_model(x)
    _, action, log_prob = sample_action(mean, ppo_model.log_std)

    credit_mult = float(0.2 + (action[0][0] + 1) / 2 * (2.0 - 0.2))
    interest_adj = float(-0.1 + (action[0][1] + 1) / 2 * 0.4)

    return {
        "creditMultiplier": credit_mult,
        "interestDelta": interest_adj,
        "logProb": float(log_prob.item()),
        "value": float(value.item())
    }
