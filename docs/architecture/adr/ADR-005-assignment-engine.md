# ADR-005: Distributed Locking for Assignment Engine

## Status
Accepted

## Context
Multiple orchestrator workers might attempt to assign the same collection case to an agent simultaneously.

## Decision
Implement a distributed lock in Redis (`LockTakeAsync`) to safely serialize case assignments.

## Consequences

### Pros
- Completely avoids race conditions
- Guarantees data consistency across the fleet
- Safely scales across multiple nodes

### Cons
- Added architectural complexity
- Requires careful handling of lock expirations/ttl

## Alternatives Considered
- DB-level locks (rejected due to poor performance and deadlocks)
- No locks (rejected due to guaranteed bugs/double-assignments)
