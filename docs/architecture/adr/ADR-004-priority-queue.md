# ADR-004: Redis ZSET + HASH for Collections Priority Queue

## Status
Accepted

## Context
We must prioritize and sort loans in real-time without duplication or inconsistencies for the Collections operations queue.

## Decision
Use:
- ZSET -> rankings sorted by `PriorityScore`
- HASH -> fast-changing metadata mapped by loan

## Consequences

### Pros
- Highly efficient updates
- Zero duplicates
- Clear separation of score vs. payload metadata

### Cons
- Added structural complexity
- Requires keeping two Redis structures synchronized

## Alternatives Considered
- JSON payload in ZSET member (rejected due to immutability of the payload breaking dynamic updates)
- DB-based ranking (rejected due to latency)
