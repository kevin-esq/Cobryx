# API Documentation Standards

This document defines the mandatory standards for documenting the Cobryx API. Adherence ensures a professional, predictable, and machine-readable developer experience via Swagger/OpenAPI.

---

## 1. Mandatory XML Documentation

Every public-facing class (Controller, DTO, Request) and public method (Action) MUST have XML documentation.

### Controllers
- **Summary**: Concise description of the controller's responsibility.
- **Tags**: Use `[Tags]` to group controllers logically in Swagger.

### Controller Actions (Endpoints)
- **Summary**: Clear one-sentence description of the action.
- **Remarks**: Used for detailed business context and **listing possible Outcome Codes**.
- **Returns**: Describe the successful result.
- **Response Codes**: Use `[ProducesResponseType]` for all 2xx, 4xx, and 5xx outcomes.
- **CancellationToken**: Every action must Receive a `CancellationToken ct`. It must be documented as follows to prevent Swagger from rendering it as a user-input field:
  ```xml
  /// <param name="ct">Injected by ASP.NET to handle request cancellation.</param>
  ```

---

## 2. Outcome Code Protocol

Outcome codes are the primary way consumers handle business results. They must be explicitly documented in the `<remarks>` section of every endpoint following the namespaced pattern `MODULE.ENTITY.ACTION_RESULT`.

### Pattern:
```xml
/// <remarks>
/// Possible Outcomes:
/// - CRM.CUSTOMER.CREATED_SUCCESS: Documentation of when this happens.
/// - AUTH.USER.LOGIN_FAILURE: Description of the failure condition.
/// </remarks>
```

---

## 3. Request/Response Contracts (DTOs)

Providing realistic data and clear validation metadata is mandatory for all primary Request and Response DTOs.

### The "Verbose Record" Pattern (Mandatory for Records)
Standard primary constructor parameter documentation (`/// <param name="...">`) often fails to display examples correctly in Swagger. **Always use explicit properties with separate summary and example tags.**

#### Correct Pattern for Request DTOs:
```csharp
public record ResendVerificationRequest([Required] string Email, string? Locale = "es-MX")
{
    /// <summary>User's registered email address.</summary>
    /// <example>owner@acme.mx</example>
    public string Email { get; init; } = Email;

    /// <summary>Preferred language for the email communication.</summary>
    /// <example>en-US</example>
    [DefaultValue("es-MX")] 
    public string? Locale { get; init; } = Locale;
}
```

> [!IMPORTANT]
> **Record Validation Rules:**
> 1. **Runtime Stability**: In .NET 8, `[Required]` must be placed on the **constructor parameter** of the record to ensure `ModelState.IsValid` and FluentValidation correctly identify mandatory fields in `init`-only properties.
> 2. **Swagger Documentation**: Summary and Example XML tags must remain on the **explicit properties** within the record body to ensure correct rendering in the Swagger UI.
> 3. **Compact Syntax**: Using `[property: Required]` on a positional record parameter is officially permitted as a shorthand, but the "Verbose Pattern" above is preferred for complex DTOs requiring detailed documentation.

### Collection Documentation
For properties of type `IEnumerable<T>`, `List<T>`, or `PaginatedList<T>`, specify limits in `<remarks>`:
```csharp
/// <summary>List of recent invoices.</summary>
/// <remarks>Returns the last 10 invoices by default. Max allowed per request: 50.</remarks>
public List<InvoiceContract> Invoices { get; init; } = Invoices;
```

### JSON Examples (<example> tag)
- Use **realistic** data (e.g., actual names, real-looking UUIDs, valid ISO dates).
- Never use generic placeholders like `string` or `0`.
- **Enums**: Since we use `JsonStringEnumConverter`, document valid values in `<remarks>`. Ensure casing matches the project configuration (typically PascalCase or camelCase).

---

## 4. Error Standards

Professional error responses must help developers debug without private data leakage.

### TraceId Format
Always use the **W3C Trace Context** format for `TraceId` examples (32-character hexadecimal).
- **Correct Example**: `<example>4bf92f3577b34da6a3ce929d0e0e4736</example>`

### Outcomes in Errors
Ensure `ApiResponse` envelopes in examples include the `TraceId` and the specific `OutcomeCode` expected for that failure type.

---

## 5. Security & Privacy

- **Sensitive Fields**: Never include sensitive data (PII, tokens, passwords) in `<example>` tags.
- **Internal IDs**: Distinguish between `InternalId` (database increment) and `ExternalId` (GUID/Stripe Reference) in descriptions if both are present.

---

## 6. Outcome Code Versioning

Outcome codes are considered part of the API contract. Breaking changes to these codes must be handled with care:

1. **Addition**: New codes can be added to existing endpoints without a version bump.
2. **Deprecation**: If a code is no longer issued, mark it as `[DEPRECATED]` in the XML remarks for at least one minor version before removal.
3. **Renaming**: Do NOT rename existing outcome codes. Create a new code and keep the old one as an alias if necessary for backward compatibility during a transition period.

---

## 7. Maintenance Policy

The documentation is as important as the code. A pull request is considered **incomplete** if it adds an endpoint or a field without corresponding XML documentation, realistic examples, and mandatory validation attributes.
