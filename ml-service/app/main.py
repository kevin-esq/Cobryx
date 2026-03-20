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

@app.post("/rl/ppo/decide")
def decide_single(payload: dict):
    import torch
    state = payload.get("state", {})
    # Map 6 EconomyState vars to the 16 model dims, padding rest with 0
    x = torch.zeros(1, 16)
    x[0,0] = state.get("avgPd", 0.5)
    x[0,1] = state.get("exposure", 0) / 10000000.0
    x[0,2] = state.get("liquidity", 0) / 500000.0
    x[0,3] = state.get("inflation", 0) / 0.2
    x[0,4] = state.get("interestRate", 0) / 0.2
    x[0,5] = state.get("unemployment", 0) / 0.1

    # Phase 20: Regime One-Hot Encoding
    regime = state.get("regime", 0)
    if regime == 0: x[0, 6] = 1.0 # Normal
    elif regime == 1: x[0, 7] = 1.0 # HighInflation
    elif regime == 2: x[0, 8] = 1.0 # Crisis
    elif regime == 3: x[0, 9] = 1.0 # Recovery

    with torch.no_grad():
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

@app.post("/rl/ppo/train")
def train(batch: list):
    import torch
    if not batch: return {"loss": 0.0}

    # Simplified stub for PPO Update
    states = []
    actions = []
    rewards = []
    dones = []

    for b in batch:
        st = b.get("state", {})
        sf = torch.zeros(16)
        sf[0] = st.get("avgPd", 0.5)
        sf[1] = st.get("exposure", 0) / 10000000.0
        sf[2] = st.get("liquidity", 0) / 500000.0
        sf[3] = st.get("inflation", 0) / 0.2
        sf[4] = st.get("interestRate", 0) / 0.2
        sf[5] = st.get("unemployment", 0) / 0.1

        regime = st.get("regime", 0)
        if regime == 0: sf[6] = 1.0
        elif regime == 1: sf[7] = 1.0
        elif regime == 2: sf[8] = 1.0
        elif regime == 3: sf[9] = 1.0

        states.append(sf)

        act = b.get("action", {})
        actions.append([act.get("creditMultiplier", 1.0), act.get("interestDelta", 0.0)])
        rewards.append(b.get("reward", 0.0))
        dones.append(b.get("done", False))

    states_t = torch.stack(states)
    actions_t = torch.tensor(actions).float()
    rewards_t = torch.tensor(rewards).float()

    # In a full RL imp, we compute advantages and optimize ppo_model
    loss = 0.5 # dummy stub

    return {"loss": loss}
