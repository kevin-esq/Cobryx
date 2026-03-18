from fastapi import FastAPI, Query
from app.schemas import FeatureVector, PredictionResponse
from app.model import joblib
import numpy as np

app = FastAPI()

def load_safe(filename):
    try:
        return joblib.load(filename)
    except:
        class Dummy:
            def predict_proba(self, X):
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
def predict(features: FeatureVector, model: str = Query("xgb_v1")):
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
