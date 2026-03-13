using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCorrelationIdToOutbox : Migration
    {
        private static readonly string[] columns = new[] { "TenantId", "IsActive" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CorrelationId",
                table: "OutboxEvents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_IsActive",
                table: "Users",
                columns: columns);

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId_IsDeleted",
                table: "Loans",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_CreatedAt",
                table: "Invoices",
                columns: new[] { "TenantId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Loans_TenantId_IsDeleted",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_CreatedAt",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "OutboxEvents");
        }
    }
}
