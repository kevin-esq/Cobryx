# Cobryx - Credit Control & Sales SaaS

## Product Vision

SaaS designed for **informal lenders, installment sellers, and small businesses**.
Objective: Clear, simple, and reliable control of customers, loans, payments, and penalties.
Focus: **Robust Backend, Clean Architecture, Scalability**.

---

## 🏗 Architecture

**Clean Architecture + DDD light + Simple CQRS**

```text
API
 ├── Controllers        → Standardized Envelope Responses (ApiResponse<T>)
Application
 ├── UseCases           → Application rules (MediatR Handlers)
Domain
 ├── Entities           → Pure business rules (User, Tenant, Credit, Payment...)
Infrastructure
 ├── Persistence        → EF Core + Npgsql
 ├── Caching            → Redis (Distributed Cache)
 ├── Security           → JWT, Claims, ClamAV (Anti-virus)
```

### Key Technical Features

- **Standardized API Responses**: All endpoints return a consistent `ApiResponse<T>` envelope.
- **Global Error Handling**: Centralized `IExceptionHandler` returning `ProblemDetails` like structures within the envelope.
- **Multi-tenancy**: Global query filters and middleware for data isolation.
- **Performance**:
    - **Redis Caching**: Distributed caching for high-traffic endpoints.
    - **Composite Indexes**: Optimized database queries.
    - **Connection Pooling**: Tuned Npgsql configuration.
- **Security**:
    - Automatic IP & Device Fingerprinting.
    - **HttpOnly Cookies**: Secure refresh token storage.
    - **Session Management**: Full control over active sessions (Revoke, Logout All).
    - ClamAV integration for file uploads.
    - Role-based Access Control (RBAC).

---

## 🛠 Technology Stack

- **.NET 8**
- **PostgreSQL** (Supabase)
- **Redis** (StackExchange.Redis)
- **Docker & Docker Compose**
- **ASP.NET Identity** (JWT)
- **Serilog** (Structured Logging)

---

## 🚀 Development Roadmap Status

### Phase 1: Foundation (✅ Completed)
- [x] **Base Repository**: Solution structure, Layers.
- [x] **Base Domain**: Tenant, User, ValueObjects.
- [x] **Architecture**: Dependency Injection, MediatR.

### Phase 2: Business Core (✅ Completed)
- [x] **Customers**: CRUD, Search, Sorting.
- [x] **Credits**: Interest logic, Amortization schedules.
- [x] **Payments**: Partial payments, logic for capital/interest allocation.
- [x] **Invoices**: Tax calculation, generation.

### Phase 3: Real Multi-tenancy (✅ Completed)
- [x] **Tenant Isolation**: Middleware, Global Query Filters.
- [x] **Branding**: Business configuration.

### Phase 4: Infrastructure (✅ Completed)
- [x] **Database**: PostgreSQL Migrations.
- [x] **Docker**: Dockerfile, docker-compose.
- [x] **Caching**: Redis implementation.

### Phase 5: Production & Security (✅ Completed)
- [x] **Security**: JWT, Roles, IP Detection.
- [x] **Observability**: Serilog, HealthChecks (API, DB, Redis, ClamAV).
- [x] **Standardization**: Uniform API Responses.

### Phase 6: Enterprise Hardening (✅ Completed)
- [x] **Advanced Auth**: HttpOnly Cookies, Token Revocation, Session Families.
- [x] **Security**: Token Reuse Detection, Account Lockout, Device Fingerprinting.
- [x] **Testing**: Comprehensive Integration Tests (Auth, Cookies, persistence).
- [x] **Code Quality**: "Nuclear" cleanup of debug artifacts and comments.

---

## 💻 How to Run

1. **Prerequisites**: Docker Desktop, .NET 8 SDK.
2. **Start Infrastructure**:
   ```bash
   docker-compose up -d
   ```
3. **Run API**:
   ```bash
   dotnet watch run --project Cobryx.Api
   ```
4. **Run Tests**:
   ```bash
   dotnet test Cobryx.IntegrationTests
   ```
5. **Access Swagger**: `http://localhost:5142/swagger`

---

## 📡 API Response Contract

Cobryx follows a strict, unified response contract to ensure stability and predictability for clients.

### 1. ApiSuccessResponse

Returned for successful operations (`200 OK`, `201 Created`).

```json
{
  "success": true,
  "message": null,
  "data": { ... },
  "outcomeCode": "AUTH.LOGIN.COMPLETED",
  "traceId": "00-12345..."
}
```

*   **`outcomeCode`**: A stable, machine-readable code indicating the business state (e.g., `MFA_REQUIRED`, `SEARCH.COMPLETED`).
*   **Zero-Text Policy**: Success responses do not return human-readable messages. The `message` field is typically `null`. The frontend determines the UI feedback based on the `outcomeCode`.

### 2. ApiErrorResponse

Returned for failed operations (`4xx`, `5xx`). Note: Specific error details are **stable codes**, not human-readable text.

```json
{
  "success": false,
  "message": "Validation Failed",
  "errorCode": "VALIDATION.FAILED",
  "numericCode": 1001,
  "errors": {
    "Email": [
      {
        "errorCode": "VALIDATION.AUTH.EMAIL.INVALID",
        "params": { "Email": "invalid-value" }
      }
    ],
    "Password": [
      { "errorCode": "VALIDATION.AUTH.PASSWORD.TOO_SHORT", "params": { "MinLength": 12 } }
    ]
  },
  "traceId": "00-12345..."
}
```

*   **`errorCode`**: A top-level programmatic code for the overall error.
*   **`numericCode`**: A unique integer for specialized client-side mapping.
*   **`errors`**: A dictionary where keys are field names and values are arrays of structured error objects.
    *   **`errorCode`**: Stable, programmatic validation code (e.g., `VALIDATION.CUSTOMER.PHONE.INVALID`).
    *   **`params`**: Contextual values (e.g., minimum length, current input) for frontend i18n interpolation.

> [!IMPORTANT]
> The backend **does not** return human-readable error messages for specific validation rules or business failures. The frontend is exclusively responsible for translating error codes using its i18n system.

---

## 🛡 Error vs Outcome Contract

*   **Errors**: Represent a failure in the business or domain logic. They always result in a **non-2xx** status code.
*   **Outcomes**: Represent a successful request that resulted in a specific, expected business state. They always result in a **2xx** status code.
