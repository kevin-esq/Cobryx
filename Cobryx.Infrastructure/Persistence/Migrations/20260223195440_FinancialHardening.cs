using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinancialHardening : Migration
    {
        private static readonly string[] columns = new[] { "TenantId", "ReferenceId" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_ReferenceId",
                table: "LedgerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_StripePaymentIntentId",
                table: "PaymentLinks",
                column: "StripePaymentIntentId",
                unique: true,
                filter: "\"StripePaymentIntentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_TenantId_ReferenceId",
                table: "LedgerTransactions",
                columns: columns,
                unique: true,
                filter: "\"ReferenceId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentLinks_StripePaymentIntentId",
                table: "PaymentLinks");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_TenantId_ReferenceId",
                table: "LedgerTransactions");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_ReferenceId",
                table: "LedgerTransactions",
                column: "ReferenceId");
        }
    }
}
