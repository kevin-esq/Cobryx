from fastapi import FastAPI
from app.schemas import FeatureVector, PredictionResponse
from app.model import Model

app = FastAPI()
model = Model()

@app.get("/health")
def health():
    return {"status": "ok"}

@app.post("/predict", response_model=PredictionResponse)
def predict(features: FeatureVector):
    pd_score = model.predict(features.dict())

    return PredictionResponse(
        probabilityOfDefault=pd_score,
        modelVersion=model.version
    )
