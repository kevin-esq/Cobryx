import pandas as pd
import xgboost as xgb
from sklearn.model_selection import train_test_split
from sklearn.preprocessing import StandardScaler
from sklearn.pipeline import Pipeline
from sklearn.metrics import roc_auc_score
import joblib

print("1. Load dataset")
df = pd.read_csv("data/dataset.csv")

X = df.drop(columns=["default"])
y = df["default"]

print("2. Split")
X_train, X_test, y_train, y_test = train_test_split(
    X, y, test_size=0.2, random_state=42
)

print("3. Pipeline")
pipeline = Pipeline([
    ("scaler", StandardScaler()),
    ("model", xgb.XGBClassifier(
        n_estimators=100,
        max_depth=4,
        learning_rate=0.1,
        subsample=0.8,
        colsample_bytree=0.8,
        eval_metric="logloss"
    ))
], memory=None)

print("4. Train")
pipeline.fit(X_train, y_train)

print("5. Evaluate")
preds = pipeline.predict_proba(X_test)[:, 1]
auc = roc_auc_score(y_test, preds)

print(f"AUC: {auc:.4f}")

print("6. Save model")
joblib.dump(pipeline, "model.pkl")
