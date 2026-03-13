# ADR-004: Shadow Replay Engine (SRE)

## Status
Accepted

## Context
Financial systems must prove their integrity. Even with the best code, data corruption or bugs in the projection logic can lead to "ghost balances." We need an independent verification mechanism.

## Decision
Implement a **Shadow Replay Engine (SRE)**. SRE consumes the same event stream as the primary system but reconstructs its own "Shadow Balance" state in a separate schema. It periodically compares its state with the primary Ledger to detect drift.

## Consequences
- **Pros**: Continuous mathematical verification of the core engine; early detection of logic bugs or corruption.
- **Cons**: Doubling of storage and compute for the "shadow" state.
