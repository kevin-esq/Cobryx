# Cobryx API

Cobryx is a modular, multi-tenant backend platform built with .NET, following Clean Architecture principles, explicit error contracts, and machine-readable API outcomes.

## Architecture Overview

Cobryx follows Clean Architecture with strict separation of concerns:

- **Cobryx.Api**: HTTP layer, request/response contracts, error/outcome mapping
- **Cobryx.Application**: Use cases, business workflows, validation
- **Cobryx.Domain**: Core domain, entities, domain rules, domain exceptions
- **Cobryx.Infrastructure**: Persistence, external services, security, background jobs

## Design Principles

The following rules are enforced across the codebase:

- The Application layer is HTTP-agnostic
- Domain and Application layers never return human-readable messages
- All errors are represented using stable error codes
- All success responses expose explicit outcome codes
- Exceptions are reserved for exceptional domain or infrastructure failures
- Expected flows use Result-based control flow

## API Contracts

Cobryx uses explicit, machine-readable contracts for both errors and successful outcomes.

- Errors are returned using stable error codes and structured metadata
- Success responses return semantic outcome codes
- No human-readable messages are exposed by the backend

Detailed documentation:
- [Error Handling](docs/api-contract/errors.md)
- [Validation Errors](docs/api-contract/validations.md)
- [API Outcomes](docs/api-contract/outcomes.md)

## Getting Started

### Prerequisites
- .NET SDK 8+
- Docker & Docker Compose

### Run locally

```bash
docker-compose up -d
dotnet run --project Cobryx.Api
```

## Testing

```bash
dotnet test
```

Integration tests validate:

* Error mapping and contracts
* Session and tenant enforcement
* Authentication and MFA flows

## Contribution Guidelines

Before adding new features:

- Do not introduce human-readable messages in backend responses
- Always use centralized error or outcome catalogs
- Do not throw raw `Exception`
- New validators must define explicit error codes
- All new API endpoints must return an outcomeCode on success

## Non-goals

- The API does not handle localization
- The API does not format user-facing messages
- The API does not expose internal exception details
