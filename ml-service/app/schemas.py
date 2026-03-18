from pydantic import BaseModel

class FeatureVector(BaseModel):
    utilization: float
    paymentDelay: float
    behaviorScore: float
    dpdTrend: float
    outstanding: float

class PredictionResponse(BaseModel):
    probabilityOfDefault: float
    modelVersion: str
