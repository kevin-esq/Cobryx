# Observability Contract

This document defines the elite-level observability standards for the Cobryx API. Adherence ensures high-fidelity monitoring, automated alerting, and bank-grade business intelligence.

---

## 1. Zero-Text Policy (Logging & Metrics)

We do not log or measure human-readable strings. All events are identified by stable, code-based identifiers. Prosaic logs (e.g., "User failed to login") are strictly prohibited.

### Outcome Code Namespacing
Codes must follow a strict three-tier hierarchy for automated aggregation and discoverability in observability tools: `MODULE.ENTITY.ACTION_RESULT`.

| Segment           | Purpose                                   | Example                                |
| ----------------- | ----------------------------------------- | -------------------------------------- |
| **MODULE**        | High-level system area (low cardinality). | `BILLING`, `AUTH`, `LENDING`           |
| **ENTITY**        | Domain object being acted upon.           | `INVOICE`, `USER`, `CREDIT`            |
| **ACTION_RESULT** | Specific outcome of the operation.        | `PAYMENT_SUCCESS`, `VALIDATION_FAILED` |

**Correct**: `BILLING.INVOICE.PAYMENT_FAILED`
**Incorrect**: `BILLING.FAILED_PAYMENT`

---

## 2. Severity Mapping & Alerting

Logging levels must correspond to the impact on the system to avoid "alert fatigue."

| Outcome Category    | HTTP Code | Serilog Level | Alerting Threshold                      |
| ------------------- | --------- | ------------- | --------------------------------------- |
| **Success**         | 2xx       | `Information` | None                                    |
| **Business Error**  | 4xx       | `Warning`     | Rate-based (e.g., >10% failure in 5m)   |
| **System Error**    | 5xx       | `Error`       | Immediate (e.g., >1 occurrence in 1m)   |
| **Security Threat** | 401/403   | `Critical`    | Immediate (e.g., Brute Force Detection) |

---

## 3. Metrics Standards

We expose metrics via OpenTelemetry. Every counter must prioritize **Success Rate** calculation.

### `cobryx_business_outcomes_total` (Counter)
- **Labels**:
  - `code`: The full Namespaced OutcomeCode.
  - `module`: The `CobryxModule` enum name.
  - `is_success`: `true` if operation completed successfully, `false` otherwise.
- **Goal**: Enable direct calculation: `rate(outcomes{is_success="false"}[5m]) / rate(outcomes[5m])`

### `cobryx_api_latency_seconds` (Histogram)
- **Labels**: `code`, `module`, `method`, `path`.
- **Goal**: Measure the "Happy Path" vs "Error Path" performance.

---

## 4. Distributed Tracing (OpenTelemetry)

Correlating logs with traces is mandatory.
- **Span Attributes**: Every span must have the `outcome_code` and `error_code` injected as attributes.
- **Baggage**: Propagate the `TenantId` and `CorrelationId` across all internal service calls.

---

## 5. Outcome Middleware & Short-Circuiting

All API responses must be intercepted by a centralized **Observability Middleware**.
- **Capability**: The middleware must capture outcomes even when the pipeline short-circuits (e.g., Auth failure, Middleware exception).
- **Default**: If a request terminates without a defined code, the middleware must assign `SYSTEM.OPERATION.UNHANDLED_ERROR`.

---

## 6. Implementation Checklist

- [ ] `OutcomeCode` is the first property in JSON log output.
- [ ] Every `ACTION.SUCCESS` has a corresponding `ACTION.FAILED` sibling.
- [ ] No string interpolation in Log Templates (Rules 10).
- [ ] Business dashboards are built using `is_success` label filters.
