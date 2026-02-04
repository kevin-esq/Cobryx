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

## 📡 API Response Standard

Every response follows this structure:

```json
{
  "success": true,
  "message": "Operation completed successfully",
  "data": {
    "id": "...",
    "name": "..."
  },
  "errors": null,
  "traceId": "00-123456789..."
}
```
