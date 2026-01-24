# Cobryx - Credit Control & Sales SaaS

## Product Vision

SaaS designed for **informal lenders, installment sellers, and small businesses**.
Objective: Clear, simple, and reliable control of customers, loans, payments, and penalties.
Focus: **Robust Backend, Clean Architecture, Scalability**.

---

## Architecture

**Clean Architecture + DDD light + Simple CQRS**

```
API
 ├── Controllers        → Orchestrate requests ONLY
Application
 ├── UseCases           → Application rules
Domain
 ├── Entities           → Pure business rules (User, Tenant, Credit, Payment...)
Infrastructure
 ├── Persistence        → EF Core
```

### Principles

- **SOLID**, **Clean Code**, **Multi-tenancy**.

---

## Technology Stack

- **.NET 8**
- **PostgreSQL**
- **Docker**
- **ASP.NET Identity** (JWT)

---

## Development Roadmap

### Phase 1: Foundation

- [ ] **Task 1: Base Repository**
  - .NET Solution, Layers, .gitignore.
- [ ] **Task 2: Base Domain**
  - Tenant, User, ValueObjects (Money).
- [ ] **Task 3: Architecture**
  - Dependency Injection, conventions.

### Phase 2: Business Core

- [ ] **Task 4: Customers**
  - Customer Entity, CRUD.
- [ ] **Task 5: Credits**
  - Interest logic, payment schedule.
- [ ] **Task 6: Payments**
  - Partial payments, capital/interest priority.

### Phase 3: Real Multi-tenancy

- [ ] **Task 7: Tenant Isolation**
  - Global TenantId, Middleware.
- [ ] **Task 8: Branding**
  - Business visual configuration.

### Phase 4: Infrastructure

- [ ] **Task 9: Database**
  - PostgreSQL, Migrations.
- [ ] **Task 10: Docker**
  - Dockerfile, docker-compose.

### Phase 5: Production

- [ ] **Task 11: Security** (JWT, Roles)
- [ ] **Task 12: Observability** (Logs)

### Phase 6: Quality

- [ ] **Task 13: Tests**
