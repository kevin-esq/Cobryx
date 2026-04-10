# Architecture Guide

## Quick Reference

### Adding a New Feature

Every new request **MUST** follow this pattern:

```csharp
// ✅ CORRECT - New feature template
[TenantScoped]
public record CreateInvoiceCommand(
    Guid CustomerId,
    decimal Amount
) : IRequest<Guid>, IRequiresTenant;

public class CreateInvoiceCommandHandler : IRequestHandler<CreateInvoiceCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateInvoiceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateInvoiceCommand request, CancellationToken ct)
    {
        // Handler logic here - NO direct DbContext, use interface
        return Guid.NewGuid();
    }
}
```

### Checklist (Definition of Done)

Before merging any PR:

- [ ] **Naming**: Commands end with `Command`, Queries end with `Query`
- [ ] **Tenant Policy**: `[TenantScoped]` attribute added
- [ ] **Interface**: Implements `IRequiresTenant`
- [ ] **Handler Purity**: No direct `DbContext`, use `IApplicationDbContext`
- [ ] **Tests Pass**: `dotnet test`
- [ ] **No Baseline Increase**: Check PR comment for delta

### Architecture Rules

| Layer          | Can Reference       | Cannot Reference            |
| -------------- | ------------------- | --------------------------- |
| Domain         | Nothing             | Application, Infrastructure |
| Application    | Domain              | Infrastructure              |
| Infrastructure | Domain, Application | -                           |
| API            | All                 | -                           |

### Current Metrics

Check the [Architecture Dashboard](./architecture-dashboard.html) for:

- 🏆 **Score** - Overall architecture health
- 📈 **Coverage** - Tenant policy adoption
- 🔥 **Hotspots** - Modules needing attention
- 🚀 **Next Action** - What to fix first

### Migration Strategy

**Don't fix everything at once.**

Follow the system:

1. Check hotspots in dashboard
2. Pick top suggested action
3. Fix 1-2 requests per PR
4. Repeat

### Module Ownership

| Module      | Team          | Status      |
| ----------- | ------------- | ----------- |
| Analytics   | platform-team | ✅ Enforced |
| Collections | platform-team | ✅ Enforced |
| Payments    | payments-team | 🔥 25 gaps  |
| Customers   | crm-team      | 🔥 22 gaps  |
| Billing     | billing-team  | 🔥 15 gaps  |

### Commands

```bash
# Run architecture tests
dotnet test --filter "FullyQualifiedName~Architecture"

# Check current metrics
dotnet test --filter "ReportArchitectureMetrics"

# Open dashboard
open docs/architecture-dashboard.html
```

---

> **Rule**: Leave it better than you found it.
>
> Every PR that touches a module should fix at least one gap.
