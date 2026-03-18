# ADR-003: Redis as Primary Read Model for Analytics

## Status
Accepted

## Context
Dashboard endpoints require <10ms latencies and must avoid impacting the primary relational database.

## Decision
Use Redis as the read model layer for `PortfolioMetrics` and `Collections`.

## Consequences

### Pros
- Ultra-low latency (<10ms)
- Complete offloading of DB read pressure
- Horizontal scalability

### Cons
- Eventual consistency
- Requires background jobs for synchronization

## Alternatives Considered
- Direct DB queries (rejected due to latency overhead)
- In-memory cache (rejected as it is not distributed)
