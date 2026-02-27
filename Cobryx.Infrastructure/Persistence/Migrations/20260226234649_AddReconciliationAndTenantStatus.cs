using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReconciliationAndTenantStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MaxRecoveryAttempts",
                table: "PaymentLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRecoveryAttemptAt",
                table: "PaymentLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RecoveryDeadline",
                table: "PaymentLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RecoveryFailureReason",
                table: "PaymentLinks",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RecoveryInProgress",
                table: "PaymentLinks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "LoanId",
                table: "LedgerTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AutoPayEnabled",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPaymentMethodId",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasSavedPaymentMethod",
                table: "Customers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "StripeCustomerId",
                table: "Customers",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AdminActionAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AdminUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionName = table.Column<string>(type: "text", nullable: false),
                    TargetType = table.Column<string>(type: "text", nullable: true),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminActionAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReconciliationAudits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    FromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ToUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastStripeCursor = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    LedgerBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    StripeAvailableBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    StripePendingBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    DriftDetailsJson = table.Column<string>(type: "text", nullable: true),
                    DetectedDriftsCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReconciliationAudits", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationAudits_TenantId_RunId",
                table: "ReconciliationAudits",
                columns: new[] { "TenantId", "RunId" });

            migrationBuilder.CreateIndex(
                name: "IX_ReconciliationAudits_TenantId_ToUtc",
                table: "ReconciliationAudits",
                columns: new[] { "TenantId", "ToUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminActionAudits");

            migrationBuilder.DropTable(
                name: "ReconciliationAudits");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "MaxRecoveryAttempts",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "NextRecoveryAttemptAt",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "RecoveryDeadline",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "RecoveryFailureReason",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "RecoveryInProgress",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "LoanId",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "AutoPayEnabled",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentMethodId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "HasSavedPaymentMethod",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "StripeCustomerId",
                table: "Customers");
        }
    }
}
