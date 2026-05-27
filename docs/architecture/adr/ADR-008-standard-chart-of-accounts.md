# ADR-008: Standard Chart of Accounts (Per Tenant)

## Status
Accepted (F6.1)

## Context
`FinancialPostingEngine` resolves **system accounts by code** when posting loan payments, charge-offs, recoveries, and platform fees. Tenants need a predictable, seeded chart so posting logic does not fail at runtime with missing accounts.

## Decision

Each tenant that participates in lending/billing receives the same **six system accounts** (`IsSystem = true`). Codes are stable API between domain, infrastructure seed, and posting engine.

| Code | Name (default) | Type | Role | Used for |
| --- | --- | --- | --- | --- |
| `1010` | Cash / Platform Cash | Asset | Available | Cash inflows/outflows (Stripe, wires) |
| `1210` | Principal Receivable | Asset | Receivable | Outstanding principal |
| `4010` | Interest Income | Revenue | None | Interest recognition |
| `4020` | Fee Revenue | Revenue | Fees | Late fees, platform fees |
| `5010` | Loss Expense | Expense | Loss | Charge-offs |
| `4030` | Recovery Income | Revenue | None | Post charge-off recoveries |

Constants live in code: [`StandardChartOfAccounts.cs`](../../../Cobryx.Domain/Accounting/StandardChartOfAccounts.cs).

### Seeding
- **Platform tenant**: [`DbInitializer.SeedPlatformTenantAsync`](../../../Cobryx.Infrastructure/Persistence/DbInitializer.cs) seeds all six accounts for `CobryxDefaults.PlatformTenantId`.
- **Operational tenants**: Integration tests and onboarding flows create the same codes before financial operations (see `ReconciliationAtomicityTests`, `FinancialStateEngineIntegrationTests`).

### Rules
1. `(TenantId, Code)` is **unique** (EF index).
2. System accounts (`IsSystem = true`) **cannot** be renamed/re-coded via `UpdateDetails`.
3. Currency is stored uppercase (ISO-4217, 3 letters).
4. Custom tenant accounts may be added later with `IsSystem = false`; posting engine only requires the six codes above for core lending flows.

### Example: loan payment JE
```
Dr 1010 Cash          100.00
    Cr 1210 Principal          80.00
    Cr 4010 Interest           15.00
    Cr 4020 Fees                5.00
```

## Consequences
- **Pros**: Posting engine stays simple (`First(a => a.Code == "1010")`); onboarding can validate chart completeness; F6.2 disbursement posts to known accounts.
- **Cons**: Hard-coded codes; multi-currency charts need future ADR if accounts must be currency-specific beyond the `Currency` column.

## References
- [`Cobryx.Application/Accounting/Services/FinancialPostingEngine.cs`](../../../Cobryx.Application/Accounting/Services/FinancialPostingEngine.cs) — `GetTenantSystemAccountsAsync`
- [`ADR-007-journal-entry-contract.md`](ADR-007-journal-entry-contract.md)
