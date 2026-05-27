# Cobryx Documentation

## Architecture

Standards and conventions for building features across the solution.

| Document | Description |
| --- | --- |
| [Engineering Practices](architecture/engineering-practices.md) | Error handling, Result/Outcome/DomainErrorCode patterns, controller conventions, validation responses, layer boundaries |
| [Coding Standards](architecture/coding-standards.md) | Domain modeling, value objects, configuration, logging, idempotency, naming conventions, compliance audit |

## API

Contract definitions and documentation standards for the public API.

| Document | Description |
| --- | --- |
| [Documentation Standards](api/documentation-standards.md) | XML documentation, Swagger conventions, DTO patterns, error response examples |
| [Error Codes](api/contract/errors.md) | Catalog of all `DomainErrorCode` values returned in `errorCode` |
| [Outcome Codes](api/contract/outcomes.md) | Catalog of all `Outcome` values returned in `outcomeCode` |
| [Validation Codes](api/contract/validations.md) | Validation error structure, code hierarchy, and frontend handling |
| [Monetization Contract](api/contract/monetization.md) | Plan limits, subscription status, checkout/portal flows (F4 / k6 stress test) |

## Operations

Observability, monitoring, and performance testing.

| Document | Description |
| --- | --- |
| [Observability](operations/observability.md) | Logging standards, metrics, distributed tracing, severity mapping |
| [Dashboards](operations/dashboards.md) | Grafana PromQL queries, dashboard layout, alerting rules |

## Load Tests

| Document | Description |
| --- | --- |
| [monetization-stress.js](load-tests/monetization-stress.js) | k6 stress test — contract in [monetization.md](api/contract/monetization.md) |
