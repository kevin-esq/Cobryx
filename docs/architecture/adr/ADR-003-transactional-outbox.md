# ADR-003: Transactional Outbox Pattern

## Status
Accepted

## Context
When a ledger transaction is committed, we often need to trigger downstream actions (notifications, analytics, event bus). Simply calling these services inside the database transaction (Dual Write) risk distributed failures where the DB commits but the event is never sent.

## Decision
Implement the **Transactional Outbox Pattern**. Financial events are written to a `FinancialOutboxEvent` table *within the same database transaction* as the ledger entries. A dedicated background worker then polls and publishes these events to the Event Bus (Redpanda/Kafka).

## Consequences
- **Pros**: "Exactly-once" (effectively) delivery guarantee; decupling of the ledger engine from external dependencies.
- **Cons**: Slight latency overhead between DB commit and event publication.
