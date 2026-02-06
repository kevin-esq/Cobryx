# Cobryx API Dashboards

> [!NOTE]
> **Audience**: This document is intended for engineers, SREs, and security reviewers consuming Cobryx telemetry via Grafana.

This document provides the canonical PromQL queries and layout recommendations for visualizing Cobryx API observability data in Grafana.

## Core Philosophy
- **One Request, One Outcome**: Each API request MUST emit exactly one business outcome. This is the source of truth for all throughput data.
- **Codes over Text**: All visualizations use stable `OutcomeCode` or `ErrorCode` values.
- **Strict Cardinality**: Data is filtered by the `CobryxModule` enum to prevent dashboard noise.
- **Zero-Trust for Messages**: We do not visualize human-readable error messages.

---

## 1. Global API Health Dashboard

### Total Business Outcomes (Volume)
Visualizes the throughput of the system by outcome. This panel represents **raw volume**.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total[5m])
)
```
**Recommended Visualization**: Time-series graph or Stacked Bar.  
> [!NOTE]
> Use this for throughput trends. For success/failure **rates**, use panels comparing outcomes vs errors or SLO alerts.

### Domain Error Rate
Identifies modules with high error cardinality.
```promql
sum by (module) (
  rate(cobryx_domain_errors_total[5m])
)
```
**Recommended Visualization**: Pie Chart or Horizontal Bar.

### Top 10 Errors (Numeric Code)
Quickly identify recurring issues by stable ID.
```promql
topk(10, sum by (code, numeric_code) (
  rate(cobryx_domain_errors_total[5m])
))
```
**Recommended Visualization**: Table with `numeric_code` and `code`.

---

## 2. Authentication & Security Dashboard

### Login Success Rate
Specific focus on the `Auth` module.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{module="Auth", code=~"AUTH.LOGIN.*"}[5m])
)
```

### Security Alert Funnel
Tracks MFA requirements vs. successful verifications.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{module="Auth", code=~"AUTH.(MFA|FIDO2).*"}[5m])
)
```
> [!TIP]
> Compare **MFA Initiated** vs. **MFA Verified** to detect friction in the user flow or potential credential stuffing/bypass attempts.

---

## 3. Operations & Business Dashboard

### Customer Onboarding Funnel
Tracks the journey from Signup to Verification to Onboarding.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{code=~"AUTH.(SIGNUP|EMAIL_VERIFIED)|TENANT.ONBOARDING.COMPLETED"}[1h])
)
```

### Financial Activity
Transactions processed vs. failures.
```promql
sum by (code) (
  rate(cobryx_business_outcomes_total{module="Financial"}[1h])
)
```

---

## Grafana Variables
To make these dashboards dynamic, use the following variables:

| Name | Type | Query |
| :--- | :--- | :--- |
| `module` | Query | `label_values(cobryx_business_outcomes_total, module)` |
| `code` | Query | `label_values(cobryx_business_outcomes_total{module="$module"}, code)` |

---

## Alerting Recommendations
Use the following logic for SLI/SLO alerts:

1. **Systemic Failure**: `sum(rate(cobryx_domain_errors_total{code="SYSTEM.INTERNAL_ERROR"}[2m])) > 1`
2. **Auth Failure Spike**: `sum(rate(cobryx_domain_errors_total{module="Auth"}[5m])) / sum(rate(cobryx_business_outcomes_total{module="Auth"}[5m])) > 0.1` (10% fail rate).
