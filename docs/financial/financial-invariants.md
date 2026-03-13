# Financial Invariants

To ensure absolute integrity and auditability of the Cobryx Financial Core, the following mathematical invariants must hold at all times. Any violation of these rules indicates a critical system defect or data corruption.

## 1. Zero-Sum Ledger
The ledger must always be in balance. No transaction can exist that does not sum to zero across its debits and credits.

**Invariant:**
`SUM(debit_entries) + SUM(credit_entries) == 0` (where credits are typically represented as negative numbers, or absolute sum equivalence).

## 2. Projection Equivalence
The materialized balance of a financial entity (e.g., Loan Balance) must exactly match the sum of its history in the ledger.

**Invariant:**
`Loan.OutstandingPrincipal == SUM(LedgerEntries WHERE Account == PrincipalAccount AND Reference == LoanId)`

## 3. Conservation of Payment Value
During payment allocation, the sum of all applied amounts (Principal, Interest, Fees) and any unapplied leftover must equal the total payment received.

**Invariant:**
`PrincipalApplied + InterestApplied + FeesApplied + UnappliedAmount == TotalPaymentAmount`

## 4. Sequence Continuity
Every ledger entry must have a unique, monotonically increasing `JournalSequenceId`. There shall be no gaps or repeats in the sequence under normal operating conditions.

**Invariant:**
`Entry[N].SequenceId < Entry[N+1].SequenceId`
