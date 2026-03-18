import joblib
import numpy as np

class Model:
    def __init__(self):
        self.pipeline = joblib.load("model.pkl")
        self.version = "xgb_v1"

    def predict(self, features: dict) -> float:
        X = np.array([[
            features.get("utilization", 0.0),
            features.get("paymentDelay", 0.0),
            features.get("behaviorScore", 0.0),
            features.get("dpdTrend", 0.0),
            features.get("outstanding", 0.0)
        ]])

        prob = self.pipeline.predict_proba(X)[0][1]

        return float(max(0.0, min(1.0, prob)))
