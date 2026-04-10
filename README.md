# Cobryx — Fintech Operations Engine

Cobryx is a high-performance fintech backend designed for lending, analytics, and collections automation.

---

## Architecture Overview

The system is structured in layered architecture:

Infrastructure → Financial Core → Domain Services → Analytics → Collections Engine → API

---

## ⚙ Core Concepts

### 1. Financial Core
- Double-entry ledger
- Idempotent transactions
- Event-driven architecture

### 2. Analytics Engine (Phase 7)
- LoanBalanceSnapshots (event-driven)
- PortfolioMetricsDaily (batch aggregation)
- Redis read model (<10ms)

### 3. Collections Engine (Phase 8)
- Strategy Engine (decision layer)
- Priority Queue (Redis ZSET)
- Assignment Engine (auto-distribution)
- AI Optimizer (ML-lite feedback loop)

---

## Data Flow

Snapshot → Strategy → CollectionCase → Redis Queue → Assignment → Action → Outcome → Optimizer → Strategy

---

## Performance Characteristics

- Reads: O(1) via Redis
- Aggregations: O(n) via SQL Window Functions
- Assignment: O(log n) via Redis ZSET

---

## Tech Stack

- .NET 8 / EF Core
- PostgreSQL
- Redis
- Hangfire

---

## Documentation

See `/docs/architecture`:

- `/docs/architecture/adr` → Architectural Decisions
- `architecture.md`
- `analytics-engine.md`
- `collections-engine.md`

---

## Testing

```bash
dotnet test
```

---

## Running

```bash
dotnet run
```

---

## Philosophy

Cobryx is designed as:
- Event-driven
- Read-optimized
- Horizontally scalable
- Self-improving (AI Optimizer)

---

## Goal

Transform financial data into real-time operational intelligence.
