using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FinalRiskEngineAuditPhase9C : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "LatestLoanSnapshots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "LatestLoanSnapshots",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "idx_snapshot_recent",
                table: "LoanBalanceSnapshots",
                column: "RecordedAt",
                descending: new bool[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_snapshot_recent",
                table: "LoanBalanceSnapshots");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "LatestLoanSnapshots");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "LatestLoanSnapshots");
        }
    }
}
