# Architecture Weekly Review

**Duration**: 10-15 minutes
**Frequency**: Monday (start of week)
**Who**: Tech leads + anyone interested

---

## Agenda (5 steps)

### 1. Score Check (1 min)

```bash
open docs/architecture-dashboard.html
```

> "This week: **Score 23** (was 21, **+2** ✅)"

### 2. Velocity Check (1 min)

> "Velocity: **0.67%/wk**, we need **2.33%/wk**"
>
> "We're **3.5x behind** but **improving** ↑"

### 3. Hotspot Owner (2 min)

> "**Payments** is still the hotspot with **25 gaps**"
>
> "@payments-team, what PRs do you have this week that can fix 2-3?"

### 4. Wins (2 min)

Celebrate PRs that improved metrics:

> "PR #142 by @dev increased coverage +1% 🎉"
>
> "PR #145 fixed 3 naming violations"

### 5. Next Actions (2 min)

From dashboard:

> "Top action: `CreateInvoiceCommand` (+0.8%)"
>
> "Who's taking it this week?"

---

## Key Phrases (always use)

| Situation   | Phrase                                          |
| ----------- | ----------------------------------------------- |
| PR improves | "This PR increases coverage +X%"                |
| PR neutral  | "Can we fix something while we're there?"       |
| PR worsens  | "This lowers the score, is there a way around?" |
| Hotspot     | "Payments is still at 0%, needs attention"      |
| Velocity    | "We're X slower than required"                  |
| Win         | "3-week streak improving 🔥"                    |

---

## Metrics to Track

| Metric       | Goal     | Current      |
| ------------ | -------- | ------------ |
| Score        | 60       | 23           |
| Coverage     | 30%      | 2%           |
| Velocity     | 2.33%/wk | 0.67%/wk     |
| Hotspot gaps | ↓        | Payments: 25 |

---

## Unwritten Rules

1. **No neutral PRs** - If you touch a module, improve something
2. **Celebrate wins** - Small progress matters
3. **Flag regressions** - No drama, just data
4. **Top action first** - The system already prioritized

---

## How to Know We Won

When you hear this **without you saying it**:

- [ ] "This PR lowers the score"
- [ ] "Let's not break the streak"
- [ ] "Take the Payments one first"
- [ ] "How much did we improve this week?"

**When that happens → the system no longer depends on you.**

---

> The goal is not to fix everything.
>
> It's to make it impossible to break again,
> and leave the system better with every change.
