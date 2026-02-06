using System.ComponentModel;

namespace Cobryx.Domain.Enums;

public enum CobryxModule
{
    [Description("Authentication & Authorization")]
    Auth,

    [Description("User Management")]
    User,

    [Description("Tenant Management")]
    Tenant,

    [Description("Customer Management")]
    Customer,

    [Description("Credits & Ledger")]
    Credits,

    [Description("Product & Catalog")]
    Product,

    [Description("Financial & Payments")]
    Financial,

    [Description("Support & Helpdesk")]
    Support,

    [Description("System & Infrastructure")]
    System,

    [Description("Other / Unknown")]
    Other
}
