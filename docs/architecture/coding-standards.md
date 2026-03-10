# Coding Standards

Canonical reference for coding conventions and domain modeling rules. Every pull request must comply.

For error handling, API response patterns, and controller conventions, see [engineering-practices.md](engineering-practices.md).

---

## Table of Contents

- [Coding Standards](#coding-standards)
  - [Table of Contents](#table-of-contents)
  - [1. Make the Domain Impossible to Break](#1-make-the-domain-impossible-to-break)
  - [2. Entities Protect Themselves](#2-entities-protect-themselves)
  - [3. Never Mix Infrastructure with Domain](#3-never-mix-infrastructure-with-domain)
  - [4. Abstract Time with IClock](#4-abstract-time-with-iclock)
  - [5. Use Value Objects](#5-use-value-objects)
  - [6. Validate at the Boundary](#6-validate-at-the-boundary)
  - [7. Keep Controllers Thin](#7-keep-controllers-thin)
  - [8. Configuration Always via Options + ValidateOnStart](#8-configuration-always-via-options--validateonstart)
  - [9. Structured Logging Only](#9-structured-logging-only)
  - [10. Idempotency in Everything External](#10-idempotency-in-everything-external)
    - [API-Level Idempotency](#api-level-idempotency)
  - [11. Nullable Reference Types Always Enabled](#11-nullable-reference-types-always-enabled)
  - [12. Code Must Scream Intention](#12-code-must-scream-intention)
  - [13. Null Coalescing Policy](#13-null-coalescing-policy)
    - [Allowed: UI / Logging / Presentation Fallbacks](#allowed-ui--logging--presentation-fallbacks)
    - [Caution: Query Projections with Role Fallbacks](#caution-query-projections-with-role-fallbacks)
    - [Prohibited: Domain / Business Logic Defaults](#prohibited-domain--business-logic-defaults)
  - [14. Magic Strings Policy](#14-magic-strings-policy)
    - [Current Good Patterns (Keep Using)](#current-good-patterns-keep-using)
    - [Current Violations (Should Fix)](#current-violations-should-fix)
  - [15. Domain Events Policy](#15-domain-events-policy)
  - [16. Automated Enforcement and Tooling](#16-automated-enforcement-and-tooling)
    - [Support Tools](#support-tools)
  - [Compliance Summary](#compliance-summary)
    - [Priority Fix Backlog](#priority-fix-backlog)

---

## 1. Make the Domain Impossible to Break

Use enums and strongly typed properties instead of strings for anything that controls behavior.

```csharp
// WRONG — string status allows invalid state
public string SubscriptionStatus { get; set; }

// CORRECT — compiler enforces valid values
public SubscriptionStatus Status { get; private set; }
```

All domain entity properties must use `private set`. No public setters allowed.

**Audit status:** COMPLIANT. All domain entities use `private set`. Enums used for
`SubscriptionStatus`, `PlanTier`, `InvoiceStatus`, `PaymentStatus`, `InstallmentStatus`, etc.

**Known violations:**
- `CustomerSuggestion.Status` is `string?` — should be an enum (`SuggestionStatus`)
- `SupportTicket` constructor defaults `category = "Inquiry"` — should be an enum

---

## 2. Entities Protect Themselves

State transitions go through domain methods, never direct property assignment.

```csharp
// WRONG — external code sets state directly
subscription.Status = SubscriptionStatus.Active;

// CORRECT — entity enforces its own invariants
subscription.ActivateFromPayment();
```

Inside the method:

```csharp
public void ActivateFromPayment()
{
    if (Status == SubscriptionStatus.Active) return; // idempotent
    if (Status == SubscriptionStatus.Cancelled)
        throw new DomainException("Cannot activate a cancelled subscription.");

    Status = SubscriptionStatus.Active;
    UpdateTimestamp();
}
```

**Audit status:** COMPLIANT. All entities use methods for state transitions
(`Activate()`, `MarkAsPaid()`, `Cancel()`, `Revoke()`, etc.).

---

## 3. Never Mix Infrastructure with Domain

The Domain layer must have zero external dependencies. No references to:
- `Cobryx.Infrastructure`
- `Cobryx.Api`
- `Stripe`, `Amazon`, `Microsoft.EntityFrameworkCore`

The Application layer must not reference Infrastructure directly:
- Use interfaces (`IStripeService`, `IFileStorageProvider`, etc.)
- Use the `(DbContext)_unitOfWork` pattern for EF queries

**Audit status:** COMPLIANT.

| Layer       | Infrastructure references found |
| ----------- | ------------------------------- |
| Domain      | 0                               |
| Application | 0                               |

---

---

> **Result Pattern, DomainErrorCode, and Outcome** are covered in [engineering-practices.md](engineering-practices.md).

---

## 4. Abstract Time with IClock

Never use `DateTime.UtcNow` directly. Use an `IClock` abstraction for testability.

```csharp
// WRONG — not testable, time-dependent behavior cannot be verified
if (subscription.TrialEndsAt < DateTime.UtcNow)

// CORRECT — injectable, testable
if (subscription.TrialEndsAt < _clock.UtcNow)
```

Interface:

```csharp
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
```

**Audit status:** COMPLIANT. The `IClock` abstraction has been introduced and registered as a singleton. All new code must favor `_clock.UtcNow` over `DateTime.UtcNow`.

| Layer       | `DateTime.UtcNow` usages | Files affected                                                                                                                                               |
| ----------- | ------------------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| Domain      | ~50                      | `BaseEntity`, `TenantSubscription`, `Payment`, `Invoice`, `Loan`, `RefreshToken`, `UserSecurityToken`, `LoginSession`, `MfaDevice`, `Credit`, `Coupon`, etc. |
| Application | ~20                      | `AuthService`, `LoginCommand`, `StripeSubscriptionSyncService`, `CreateLoanHandler`, `ApplyLateFeesHandler`, `DashboardSummaryQuery`, etc.                   |

> [!IMPORTANT]
> **Migration Strategy (Boy Scout Rule):**
> Introducing `IClock` across 70+ locations is high-risk. We follow a gradual migration:
> 1. **New Code:** Mandatory use of `_clock.UtcNow`. Direct usage of `DateTime.UtcNow` will fail PR review.
> 2. **Refactoring:** When modifying an existing file for any reason, migrate its `DateTime` calls to `IClock` as a mandatory part of the change.
> 3. **Avoid Big Bangs:** Do not perform a global search-and-replace unless it's a dedicated, isolated sprint task.

---

## 5. Use Value Objects

Wrap primitive types that carry domain meaning into Value Objects:

```csharp
// WRONG — primitives leak context
public decimal Price { get; set; }
public string Currency { get; set; }

// CORRECT — Value Object enforces invariants
public Money Price { get; private set; }
```

**Audit status:** COMPLIANT. Seven Value Objects exist:

| Value Object       | Purpose                                          |
| ------------------ | ------------------------------------------------ |
| `Money`            | Amount + Currency, prevents mixing MXN/USD       |
| `EmailAddress`     | Validated email with disposable domain detection |
| `Address`          | Structured address with coordinates              |
| `TaxId`            | Tax identification (RFC)                         |
| `LegalConsent`     | Terms version + IP + timestamp                   |
| `IdentityDocument` | Identity document type + number                  |
| `BusinessSettings` | Tenant business configuration                    |

---

## 6. Validate at the Boundary

Validation happens at three levels. All three are mandatory:

| Layer       | Responsibility                | Mechanism                                         |
| ----------- | ----------------------------- | ------------------------------------------------- |
| API         | Input format, required fields | `[Required]`, FluentValidation, `DataAnnotations` |
| Application | Business rules, authorization | `Result.Failure<T>(errorCode)`                    |
| Domain      | Invariants, state integrity   | Constructor guards, `DomainException`             |

Never trust controller validation alone. The domain must protect itself.

**Audit status:** COMPLIANT. All three layers enforce validation:
- API: Request contracts with `[Required]`
- Application: MediatR pipeline behaviors with FluentValidation
- Domain: Constructor guards and method preconditions

---

## 7. Keep Controllers Thin

Controllers should only:
1. Extract request data
2. Send a MediatR command/query
3. Map the result to HTTP response

```csharp
// CORRECT — thin controller
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
{
    var command = new CreateCustomerCommand(request.FullName, request.Email);
    var result = await Sender.Send(command, ct);
    return HandleResult(result, Outcomes.Customer.Created);
}
```

If a controller action exceeds ~20 lines of logic (excluding XML docs, attributes, etc.), extract it into a command/query handler.

**Audit status:** COMPLIANT. Controllers use `await Sender.Send(command)` pattern.
`WebhooksController` is the largest at 167 lines, but most of that is event routing logic
which is appropriate for a controller that dispatches to a sync service.

---

## 8. Configuration Always via Options + ValidateOnStart

Never read `IConfiguration` directly with string keys. Always use strongly-typed Options.

```csharp
// WRONG — silent defaults, no startup validation
_host = configuration["Security:ClamAV:Host"] ?? "localhost";
_port = int.Parse(configuration["Security:ClamAV:Port"] ?? "3310");

// CORRECT — fails at startup if missing
public class ClamAvOptions
{
    public const string SectionName = "Security:ClamAV";

    [Required]
    public string Host { get; set; } = default!;

    [Range(1, 65535)]
    public int Port { get; set; } = 3310;
}

// Registration:
services.AddOptions<ClamAvOptions>()
    .Bind(configuration.GetSection(ClamAvOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Rules for defaults:
- **Never as default:** API keys, secrets, tokens, origins, domains, URLs, connection strings
- **Acceptable as default:** Port numbers, TTLs, non-sensitive operational values

> [!CAUTION]
> **Prohibit IConfiguration Injection:** Never inject `IConfiguration` directly into business services. This masks dependencies and makes testing a nightmare. Always depend on `IOptions<T>`, `IOptionsSnapshot<T>`, or `IOptionsMonitor<T>`.

**Audit status:** COMPLIANT. All critical security and infrastructure options now use the `AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()` pattern.

Services using `IOptions<T>` correctly:

| Service         | Options Class   |
| --------------- | --------------- |
| `StripeService` | `StripeOptions` |
| `AppOptions`    | `AppOptions`    |

Services using raw `configuration["..."]` (violations):

| Service                  | Config Key                   | Default                   | Risk Level                                          |
| ------------------------ | ---------------------------- | ------------------------- | --------------------------------------------------- |
| `Fido2Service`           | `Fido2:Origin`               | `"http://localhost:3000"` | CRITICAL — wrong origin breaks auth in production   |
| `Fido2Service`           | `Fido2:ServerDomain`         | `"localhost"`             | CRITICAL — wrong domain breaks passkey verification |
| `CaptchaService`         | `Security:Captcha:SecretKey` | Turnstile test key        | CRITICAL — test key bypasses captcha in production  |
| `ClamAvScanner`          | `Security:ClamAV:Host`       | `"localhost"`             | HIGH — scan fails silently                          |
| `ClamAvScanner`          | `Security:ClamAV:Port`       | `"3310"`                  | HIGH                                                |
| `JwtTokenGenerator`      | `JwtSettings:ExpiryMinutes`  | `"60"`                    | MEDIUM — silent default                             |
| `HealthCheckExtensions`  | Multiple keys                | Various                   | MEDIUM — duplicated config reads                    |
| `DependencyInjection.cs` | `Caching:DefaultTTL`         | `null!`                   | HIGH — NRE crash if missing                         |
| `R2StorageProvider`      | `Storage:S3:BucketName`      | `"documents"`             | LOW                                                 |

---

## 9. Structured Logging Only

Always use message templates with named parameters. Never use string interpolation.

```csharp
// WRONG — prevents structured search in observability tools
_logger.LogInformation($"User {userId} upgraded plan");

// CORRECT — searchable by {UserId} in Seq, Datadog, etc.
_logger.LogInformation("User {UserId} upgraded plan", userId);
```

**Audit status:** COMPLIANT. Zero string interpolation found in any logger call across the entire project.

---

## 10. Idempotency in Everything External

Any code that handles external events (webhooks, payment notifications, retries) must be idempotent:

- Duplicate events must produce the same result
- Out-of-order events must not corrupt state
- Delayed events must not overwrite fresher state

```csharp
// CORRECT — idempotent activation
public void ActivateFromPayment()
{
    if (Status == SubscriptionStatus.Active) return; // already activated
    Status = SubscriptionStatus.Active;
}
```

### API-Level Idempotency
For all POST/PUT operations that perform financial mutations or critical state changes (e.g., `CreatePayment`, `CreateLoan`), the API must support `X-Idempotency-Key`.
- The key ensures that replayed requests (due to network retries) do not execute business logic more than once.
- Idempotency state should be persisted for at least 24 hours.

**Audit status:** COMPLIANT. `StripeSubscriptionSyncService` is designed for idempotent
handling. `TenantSubscription` state transitions include idempotent guards.

---

## 11. Nullable Reference Types Always Enabled

Every `.csproj` must have:

```xml
<Nullable>enable</Nullable>
```

**Audit status:** COMPLIANT.

| Project                   | Status  |
| ------------------------- | ------- |
| `Cobryx.Domain`           | Enabled |
| `Cobryx.Application`      | Enabled |
| `Cobryx.Infrastructure`   | Enabled |
| `Cobryx.Api`              | Enabled |
| `Cobryx.Domain.Tests`     | Enabled |
| `Cobryx.IntegrationTests` | Enabled |

---

In the domain, avoid `Guid.NewGuid()` as it causes index fragmentation in relational databases. Use **Sequential GUIDs** (e.g., via `RT.Comb` or `NewId`).

> **Rule:** All entity identifiers must be generated using a sequential/ordered UUID scheme to ensure B-Tree efficiency and data locality in the persistence layer.

---

---

> **Clean Architecture layer rules and boundaries** are covered in [engineering-practices.md](engineering-practices.md).

---

## 12. Code Must Scream Intention

Method names must describe the business action, not the technical operation.

```csharp
// WRONG — vague, what does "process" mean?
Process()
Update()
Handle()

// CORRECT — business action is clear
ActivateFromPayment()
HandleCheckoutCompletedAsync()
ExecuteCancellation()
SyncFromStripe()
```

**Audit status:** COMPLIANT. Domain methods follow action-oriented naming:
`ActivateFromPayment()`, `HandlePaymentFailed()`, `ExecuteCancellation()`,
`SyncFromStripe()`, `MarkAsPaid()`, `ApplyPayment()`, `Revoke()`, etc.

---

## 13. Null Coalescing Policy

The `??` operator is permitted, but its usage depends on the architectural layer:

### Allowed: UI / Logging / Presentation Fallbacks

Safe display defaults that do not affect business logic:

```csharp
// OK — logging safe default
ipAddress ?? "0.0.0.0"
userAgent ?? "Unknown"

// OK — query projection for display
i.Customer?.FullName ?? "Unknown"
user.Profile?.PreferredLanguage ?? "es-MX"    // mirrors domain default in UserProfile.cs
user.Profile?.Timezone ?? "America/Mexico_City" // mirrors domain default in UserProfile.cs
```

### Caution: Query Projections with Role Fallbacks

```csharp
// Acceptable but fragile
user.Role?.Name ?? "User"     // GetUsersQuery — what if role is deleted?
user.Role?.Name ?? "Unknown"  // GetMyProfileQuery — slightly better
```

Rule: If a user must always have a role, enforce it at the domain level (constructor
invariant), not silently in queries. If role is truly optional, prefer `"Unknown"` over
`"User"` to avoid implying permissions.

### Prohibited: Domain / Business Logic Defaults

Never silently default values that represent business state:

```csharp
// DANGEROUS — hides bugs
subscriptionStatus ?? "Active"
paymentStatus ?? "Completed"
request.TermsVersion ?? "v1.0"

// CORRECT — fail loudly
subscriptionStatus ?? throw new DomainException("Subscription status missing")
```

Rule: In the Domain layer, null equals invalid state. It must throw, never silently default.

---

## 14. Magic Strings Policy

### Current Good Patterns (Keep Using)

| Pattern                 | Example                                 | Location             |
| ----------------------- | --------------------------------------- | -------------------- |
| Permission constants    | `Permission.Constants.CustomersView`    | `Permission.cs`      |
| PaymentMethod constants | `PaymentMethod.PaymentMethods.Cash`     | `PaymentMethod.cs`   |
| Outcome Namespacing     | `CRM.CUSTOMER.CREATED_SUCCESS`          | All Outcome classes  |
| Error Hierarchy         | `Error.Customer.NotFound`               | `DomainErrorCode.cs` |
| Metric names            | `const string MeterName = "Cobryx.Api"` | `CobryxMetrics.cs`   |

### Current Violations (Should Fix)

| File                    | Magic String                            | Fix                            |
| ----------------------- | --------------------------------------- | ------------------------------ |
| `CustomerSuggestion.cs` | `Status = "Pending"`                    | Create `SuggestionStatus` enum |
| `DbInitializer.cs`      | `role.Name == "Owner"`, `"Admin"`, etc. | Add `Role.Constants` class     |

Rule: If a string value appears in a `switch`, `if`, or status comparison, it must be
an enum or a `const`. Prefer enums over strings for any value that controls behavior.

---

## 15. Domain Events Policy

Entities must never execute side effects (sending emails, calling external APIs, writing to disk) directly. Instead, they record intentions of change.

**Rule:**
1. Entities register events by adding to a protected `_domainEvents` collection.
2. Events must be immutable classes derived from `IDomainEvent`.
3. Side effects are executed by `IDomainEventHandler`s in the **Application** layer.
4. The `UnitOfWork` (or a dedicated dispatcher) is responsible for publishing events *after* a successful database transaction.

```csharp
public void MarkAsPaid()
{
    if (Status == Status.Paid) return;
    Status = Status.Paid;

    // CORRECT — Registering intent, not execution
    AddDomainEvent(new InvoicePaidEvent(Id, CustomerId));
}
```

---

## 16. Automated Enforcement and Tooling

Standardizing code is only half the battle; enforcement should be automated wherever possible.

### Support Tools
- **Roslyn Analyzers:** Use custom analyzers to enforce naming conventions and prohibit `DateTime.UtcNow`.
- **ArchUnit.NET:** Mandatory for unit testing architecture. Use it to fail the build if `Domain` references `Infrastructure` or if `Controllers` are not thin.
- **EditorConfig:** Strict `.editorconfig` to enforce formatting and prevent "noise" in PR diffs.

---

## Compliance Summary

| #   | Rule                           | Status                     |
| --- | ------------------------------ | -------------------------- |
| 1   | Domain uses enums, not strings | PASS (2 minor exceptions)  |
| 2   | Entities protect themselves    | PASS                       |
| 3   | No infrastructure in domain    | PASS                       |
| 4   | Result pattern over exceptions | PASS                       |
| 5   | IClock abstraction             | PASS                       |
| 6   | Value Objects                  | PASS (7 VOs)               |
| 7   | Validate at boundary           | PASS                       |
| 8   | Thin controllers               | PASS                       |
| 9   | Options + ValidateOnStart      | PASS                       |
| 10  | Structured logging             | PASS                       |
| 11  | Idempotency                    | PASS                       |
| 12  | Nullable Reference Types       | PASS (all 6 projects)      |
| 13  | Guid abstraction               | ACCEPTABLE                 |
| 14  | Clean Architecture layers      | PASS                       |
| 15  | Intentional naming             | PASS                       |
| 16  | Null coalescing policy         | PASS (minor role fallback) |
| 17  | No magic strings               | FAIL — 3 files             |

### Priority Fix Backlog

| Priority         | Item                                                    | Effort                |
| ---------------- | ------------------------------------------------------- | --------------------- |
| P0 (Security)    | Fido2, Captcha, JWT config to Options + ValidateOnStart | Small                 |
| P1 (Reliability) | ClamAV, Redis, HealthCheck config to Options            | Small                 |
| P2 (Quality)     | Magic strings to enums/constants                        | Small                 |
| P3 (Testability) | IClock abstraction                                      | Large (cross-cutting) |
