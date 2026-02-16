# Cobryx API Dashboards

This document provides the canonical PromQL queries and layout recommendations for visualizing Cobryx API observability data in Grafana. Adherence to these panels ensures bank-grade monitoring and proactive incident detection.

---

## 1. Global API Health (The Golden Signals)

This row represents the high-level health of the entire system.

### Global Success Rate
**Goal**: Calculate the conversion of all business intents.
```promql
sum(rate(cobryx_business_outcomes_total{is_success="true"}[5m])) /
sum(rate(cobryx_business_outcomes_total[5m]))
```
**Visualization**: Gauge or Stat panel. Red if < 95%, Yellow if < 99%.

### P95 Latency by Outcome
**Goal**: Identify slow "Happy Paths" vs "Error Paths".
```promql
histogram_quantile(0.95, sum by (le, code) (
  rate(cobryx_api_latency_seconds_bucket[5m])
))
```
**Visualization**: Time-series graph.

### System "Dead Man's Switch"
**Goal**: Detect if the API or telemetry exporter is down.
```promql
absent(cobryx_business_outcomes_total)
```
**Alert**: Critical if `1` (indicates no outcomes emitted in the last 5 minutes).

---

## 2. Authentication & Security (Friction & Integrity)

### MFA/Passkey Conversion Ratio
**Goal**: Detect usabilty friction or credential stuffing bots.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{module="Auth", code=~"AUTH.(MFA|FIDO2).*"}[5m])
)
```
**Visualization**: Stat panel (Conversion %). Low conversion between `INITIATED` and `VERIFIED` suggests a broken flow or high-volume bot testing.

### Outcome Consistency Audit
**Goal**: Ensure every API request follows the Outcome Code protocol.
```promql
sum(rate(http_requests_total[5m])) - sum(rate(cobryx_business_outcomes_total[5m]))
```
**Target**: Must be `0`. Any non-zero value indicates endpoints missing the `ObservabilityFilter`.

---

## 3. Business Funnels

### Customer Onboarding Funnel
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{code=~"AUTH.(SIGNUP|EMAIL_VERIFIED)|TENANT.ONBOARDING.COMPLETED"}[1h])
)
```

### Financial Success Volume
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{module="Financial", is_success="true"}[1h])
)
```

---

## 4. Recommended Dashboard Layout

To ensure readability under pressure (during incidents), use this three-tier layout:

| Row | Content | Focus |
| :--- | :--- | :--- |
| **Tier 1: Global Health** | Success Rate, Latency P95, Total Throughput | Is the system healthy? |
| **Tier 2: Business Flow** | Onboarding Funnel, Financial Transactions, MFA Friction | Are users succeeding? |
| **Tier 3: Technical Health** | Top 10 Errs (Numeric), 5xx Error Logs (Loki), TraceId Search | Why is it failing? |

---

## 5. Alerting Recommendations

| Alert | Logic | Severity |
| :--- | :--- | :--- |
| **Silent Death** | `absent(cobryx_business_outcomes_total)` | **CRITICAL** |
| **Systemic Failure (5xx)** | `sum(rate(cobryx_domain_errors_total{code=~"SYSTEM.*"}[2m])) > 1` | **CRITICAL** |
| **Auth Error Spike** | `(SuccessRate < 0.90)` in module Auth | **HIGH** |
| **Business Friction** | `MFA_Initiated / MFA_Verified < 0.60` | **MEDIUM** |
