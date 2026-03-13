using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixConcurrencyTokenStrategy : Migration
    {
        /// <inheritdoc />
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "Users", "UserProfiles", "UsageRecords", "TenantSubscriptions", "Tenants",
                "TaxConfigurations", "SystemErrorLogs", "SupportTickets", "SubscriptionPlans",
                "SecurityTokens", "Roles", "ReleaseNotes", "RecoveryCodes", "Products",
                "Permissions", "Payments", "PaymentMethods", "PaymentAllocations", "MfaDevices",
                "LoginSessions", "Invoices", "InvoiceItems", "Installments", "Documents",
                "CustomerSuggestions", "Customers", "Credits", "Coupons", "BillingAlerts", "AuditLogs"
            };

            foreach (var table in tables)
            {
                migrationBuilder.AddColumn<long>(
                    name: "Version",
                    table: table,
                    type: "bigint",
                    nullable: false,
                    defaultValue: 0L);
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "Users", "UserProfiles", "UsageRecords", "TenantSubscriptions", "Tenants",
                "TaxConfigurations", "SystemErrorLogs", "SupportTickets", "SubscriptionPlans",
                "SecurityTokens", "Roles", "ReleaseNotes", "RecoveryCodes", "Products",
                "Permissions", "Payments", "PaymentMethods", "PaymentAllocations", "MfaDevices",
                "LoginSessions", "Invoices", "InvoiceItems", "Installments", "Documents",
                "CustomerSuggestions", "Customers", "Credits", "Coupons", "BillingAlerts", "AuditLogs"
            };

            foreach (var table in tables)
            {
                migrationBuilder.DropColumn(
                    name: "Version",
                    table: table);
            }
        }


    }
}
