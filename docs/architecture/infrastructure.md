# Infrastructure Overview

Global architecture of the Cobryx Financial Infrastructure.

```mermaid
flowchart TD
    API[API Gateway / Dashboard]
    Ledger[Ledger Engine]
    Outbox[Outbox Worker]
    Kafka[Event Bus - Redpanda]
    Shadow[Shadow Replay Engine]
    Redis[Balance Cache - Redis]
    DB[(PostgreSQL - Primary)]

    API -->|Commands| Ledger
    Ledger -->|Atomic Commit| DB
    Ledger -->|Write-Through| Redis
    Ledger -->|Transactional Outbox| DB
    Outbox -->|Poll| DB
    Outbox -->|Publish| Kafka
    Kafka -->|Cdc Stream| Shadow
    Shadow -->|Verify| DB
    API -->|Read Balance| Redis
```

## Component Roles
- **Ledger Engine**: Core business logic and transactional integrity.
- **Outbox Worker**: Reliable event dispatcher with retry logic.
- **Redpanda (Kafka)**: Distributed log for all financial mutations.
- **Shadow Replay Engine**: Continuous independent auditor for drift detection.
- **Redis**: High-performance materialized view for financial state.
