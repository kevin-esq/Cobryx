# Observability Contract

This document defines the observability standards for the Cobryx API.

## Core Concepts

### 1. Zero-Text Policy (Logging & Metrics)
We do not log or measure human-readable strings. All events are identified by stable, code-based identifiers:
- **OutcomeCode**: Identifies a successful business outcome (e.g., `AUTH.LOGIN.SUCCESS`).
- **ErrorCode**: Identifies a failure reason (e.g., `AUTH.INVALID_CREDENTIALS`).

### 2. Modules
All codes must be prefixed with a valid Module. The module is strictly typed using the `CobryxModule` enum to ensure consistent, low-cardinality metrics.

### 3. Outcomes on Errors
Every response (Success or Error) SHOULD contain an `OutcomeCode`.
- **Success Outcome**: `MODULE.ACTION.SUCCESS` (or similar, e.g., `CREATED`, `COMPLETED`).
- **Failure Outcome**: `MODULE.ACTION.FAILED` or `MODULE.FAILED` (as a fallback).

This allows for high-level flow analysis (Intent vs. Fulfillment) without digging into specific error details.

## Metrics Guarantees

We expose two primary counters via OpenTelemetry:

#### `cobryx_business_outcomes_total`
- **Trigger**: Every API response.
- **Labels**:
  - `code`: The `OutcomeCode`.
  - `module`: The module enum name (e.g., `Auth`, `Billing`).

#### `cobryx_domain_errors_total`
- **Trigger**: Failed API response (HTTP 4xx/5xx) or Exceptions.
- **Labels**:
  - `code`: The `ErrorCode`.
  - `module`: The module enum name.
  - `numeric_code`: The stable numeric ID of the error (optional but recommended for filtering).

## Implementation Details

- **ObservabilityFilter**: Automatically extracts codes from the `ApiResponse` body.
- **Fallback**: If no code is present, `SYSTEM.OPERATION.UNKNOWN` is used.
- **Correlation**: `ErrorCode` and `OutcomeCode` are injected into Serilog `LogContext` for correlation with traces.
