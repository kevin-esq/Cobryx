import random

class Model:
    def __init__(self):
        self.version = "v1.0.0"

    def predict(self, features: dict) -> float:
        # XGBoost simulation
        score = (
            features.get("dpdTrend", 0.0) * 0.02 +
            features.get("utilization", 0.0) * 0.4 +
            features.get("behaviorScore", 0.0) * 0.3 +
            features.get("paymentDelay", 0.0) * 0.2
        )

        score += random.uniform(-0.02, 0.02)

        return max(0.0, min(1.0, score))
