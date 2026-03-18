FROM python:3.11-slim

WORKDIR /app

RUN adduser --disabled-password appuser

COPY requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY app ./app
COPY model.pkl .

USER appuser

HEALTHCHECK CMD curl --fail http://localhost:80/health || exit 1

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "80"]
