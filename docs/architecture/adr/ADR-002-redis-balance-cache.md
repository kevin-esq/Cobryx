# ADR-002: Redis Balance Cache & Lua Atoms

## Status
Accepted

## Context
Aggregating balances directly from millions of ledger entries in Postgres for every display request (Dashboard, Mobile App) is computationally expensive and slow. We need a low-latency cache for "Current Balance" while maintaining high integrity.

## Decision
Use Redis as a materialized view for real-time balances. To prevent race conditions during concurrent cache updates, all modifications (debits/credits) are executed via **Atomic Lua Scripts** that verify the `LastSequenceId` matches the incoming update.

## Consequences
- **Pros**: Sub-millisecond balance reads; atomic protection against out-of-order updates.
- **Cons**: Introduces Redis as a critical infrastructure dependency.
