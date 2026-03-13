# ADR-001: Global Ledger Sequence ID

## Status
Accepted

## Context
In a distributed financial system, maintaining a strict, total order of transactions is critical for idempotency, auditability, and balance consistency. Without a global sequence, concurrent updates to the same account could result in race conditions or "lost updates" that are impossible to trace.

## Decision
We implement a `JournalSequenceId` (long) as a global, monotonically increasing counter for every entry in the ledger. This sequence is assigned at the database level during the transaction commit.

## Consequences
- **Pros**: Guaranteed total ordering; simplified "Shadow Replay" for consistency checks; easy drift detection.
- **Cons**: Potential lock contention on the sequence generator at extreme scale (mitigated by Postgres bigserial or sequences).
