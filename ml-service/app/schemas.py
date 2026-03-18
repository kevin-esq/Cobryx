from pydantic import BaseModel

class FeatureVector(BaseModel):
    pD: float
    utilization: float
    paymentDelay: float
    behaviorScore: float
    dpdTrend: float

class PredictionResponse(BaseModel):
    probabilityOfDefault: float
    modelVersion: str
