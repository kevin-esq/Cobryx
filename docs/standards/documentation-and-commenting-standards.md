# Elite Documentation and Commenting Standards

This document defines the elite standards for documenting and commenting the Cobryx codebase. Our goal is **100% intentionality and institutional maturity (9.8/10)**: every doc and comment must add value that the code cannot express on its own.

---

## 1. UML as Code (Mermaid.js)

For a project of this magnitude (Financial Core), visual documentation is mandatory. We use **Mermaid.js** embedded in Markdown files.

### 1.1 Diagram Types by Context
| Context | Diagram Type | Purpose |
| --- | --- | --- |
| **System Architecture** | C4 Container | Show Api, Redpanda, Redis, and Database boundaries. |
| **Business Flow** | Sequence | Document cross-service orchestration (e.g., Payment -> Ledger). |
| **Entity Lifecycle** | State | Show transition rules for `LoanStatus`, `PaymentStatus`, etc. |
| **Domain Model** | Class | Show Aggregate Roots, Entities, and Value Objects. |

### 1.2 Placement
Diagrams must live in `docs/architecture/diagrams/` (Global) or `docs/financial/` (Domain-specific). Feature-specific diagrams live in `docs/features/`.

---

## 2. Code Commenting Standards

### 2.1 The "Why" vs The "What"
*   **AVOID** describing what the code does if it's clear from naming.
    *   *Bad:* `// Check if loan is delinquent`
    *   *Good:* (Code naming) `if (loan.IsDelinquent())`
*   **MANDATORY** documentation of business rationale or non-obvious constraints.
    *   *Example:* `// We enforce sequential GUIDs here to prevent B-Tree fragmentation in the Audit log index.`

### 2.2 XML Documentation (Public & Internal)
Every public method in `Application` (Interfaces/Services) and `Domain` (Aggregates) MUST have XML tags.

#### Aggregate Root Methods (Domain)
```csharp
/// <summary>
/// Transition the loan to a Written-Off state.
/// </summary>
/// <remarks>
/// - Only delinquent loans can be written off.
/// - This action stops interest accrual and triggers a ledger charge-off entry.
/// - Once written off, the status is immutable.
/// </remarks>
public void MarkAsWrittenOff(DateTime today) { ... }
```

#### Application Services
Document the **orchestration flow**:
```csharp
/// <summary>
/// Orchestrates the end-of-day accrual batch.
/// </summary>
/// <remarks>
/// 1. Fetches all active loans.
/// 2. Iterates and calls ProcessLoanAccrualAsync (catch-up).
/// 3. Emits FinancialOutboxEvents for external synchronization.
/// </remarks>
public async Task RunDailyAccrualAsync(CancellationToken ct) { ... }
```

### 2.3 Guard Clause Documentation
If a guard clause throws or returns a specific error, explain the **Business Invariant** it protects.

### 2.4 Race Protection & Concurrency
Always comment sections that implement:
*   `SKIP LOCKED` in SQL.
*   Idempotency checks via `LastSequenceId` or `ProcessedEvents`.
*   Optimistic Concurrency (RowVersion).

---

## 4. Architecture Decision Records (ADRs)

For every critical architectural choice (e.g., Ledger Sequence, Redis materialization), we MUST maintain an ADR.
*   **Location**: `docs/architecture/adr/ADR-XXX-name.md`
*   **Requirement**: ADRs must include **Context**, **Decision**, and **Consequences**. This prevents historical amnesia.

---

## 5. Financial Invariants

Mathematical rules that govern system integrity MUST be documented in `docs/financial/financial-invariants.md`. Developers must refer to these when implementing new ledger logic.

---

## 6. Operational Runbooks

Recovery and operational checklists live in `docs/runbooks/`. These are the "action books" for production incidents (e.g., drift detection, worker failure).

---

## 7. The "Exorcism" Policy
*   Delete any `// TODO`, `// Placeholder`, or `// In a real system` comments.
*   Replace them with professional documentation if the logic is still evolving, or implement the final logic.
*   Delete commented-out code blocks. Use Git history for archeology.
