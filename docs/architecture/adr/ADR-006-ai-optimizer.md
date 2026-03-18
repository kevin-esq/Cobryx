# ADR-006: ML-lite Optimizer with Epsilon-Greedy Strategy

## Status
Accepted

## Context
Static escalation rules fail to optimize for recovery rates or operational costs dynamically.

## Decision
Implement a feedback-loop optimizer based on:
- Success Rate
- Amount Recovered
- Epsilon-Greedy Algorithm (~10% exploration chance)

## Consequences

### Pros
- Continuous, automated self-improvement
- Low computational cost
- Reaps benefits without heavy ML infrastructure

### Cons
- Does not capture highly complex non-linear patterns
- Requires historical data volume to converge

## Alternatives Considered
- Full Machine Learning pipelines (rejected due to infrastructure overkill)
- Fixed static rules (rejected due to lack of adaptive optimization)
