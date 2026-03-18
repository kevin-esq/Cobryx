# ADR-002: Use SQL Window Functions for Snapshot Aggregation

## Status
Accepted

## Context
Fetching the latest snapshot per loan using LINQ (`GroupBy`) generates inefficient queries and multiple table scans.

## Decision
Use native SQL with `ROW_NUMBER() OVER (PARTITION BY LoanId ORDER BY RecordedAt DESC)` via `FromSqlRaw`.

## Consequences

### Pros
- O(n) algorithmic complexity
- 10x–100x performance improvement
- Full control over the query execution plan

### Cons
- Dependency on specific SQL dialect
- Less pure ORM portability

## Alternatives Considered
- LINQ GroupBy (rejected due to poor performance)
- Multiple queries per LoanId (rejected due to N+1 problem)
