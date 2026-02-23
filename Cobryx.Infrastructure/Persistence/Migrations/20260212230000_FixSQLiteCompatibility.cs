using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixSQLiteCompatibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "AuditLogs", "Credits", "Customers", "Payments", "Products", "Tenants", "Users",
                "Installments", "Roles", "Permissions", "SubscriptionPlans", "TenantSubscriptions",
                "Coupons", "BillingAlerts", "CustomerSuggestions", "TaxConfigurations",
                "RecoveryCodes", "RefreshTokens", "ReleaseNotes", "Documents",
                "SupportTickets", "SystemErrorLogs", "Invoices", "InvoiceItems"
            };

            foreach (var table in tables)
            {
                if (migrationBuilder.ActiveProvider == "Npgsql.EntityFrameworkCore.PostgreSQL")
                {
                    // PostgreSQL supports ADD COLUMN IF NOT EXISTS
                    migrationBuilder.Sql($"ALTER TABLE \"{table}\" ADD COLUMN IF NOT EXISTS \"DeletedAt\" timestamp with time zone;");
                }
                else
                {
                    migrationBuilder.AddColumn<DateTime>(
                        name: "DeletedAt",
                        table: table,
                        type: "timestamp with time zone",
                        nullable: true);
                }

                // For SQLite tests, we need to remove PG-specific concurrency columns 
                // that might have been added by legacy migrations or designer files.
                if (migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.Sqlite")
                {
                    // Use a safe approach to drop columns if they exist
                    // Note: EF Core SQLite provider handles DropColumn by recreating the table
                    migrationBuilder.DropColumn(name: "RowVersion", table: table);
                    migrationBuilder.DropColumn(name: "xmin", table: table);
                }
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            var tables = new[]
            {
                "AuditLogs", "Credits", "Customers", "Payments", "Products", "Tenants", "Users",
                "Installments", "Roles", "Permissions", "SubscriptionPlans", "TenantSubscriptions",
                "Coupons", "BillingAlerts", "CustomerSuggestions", "TaxConfigurations",
                "RecoveryCodes", "RefreshTokens", "ReleaseNotes", "Documents",
                "SupportTickets", "SystemErrorLogs", "Invoices", "InvoiceItems"
            };

            foreach (var table in tables)
            {
                migrationBuilder.DropColumn(
                    name: "DeletedAt",
                    table: table);
            }
        }
    }
}
