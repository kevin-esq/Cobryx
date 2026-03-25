using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRiskEnginePhase9B : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerRiskSnapshots_CustomerId",
                table: "CustomerRiskSnapshots");

            migrationBuilder.DropIndex(
                name: "IX_CustomerRiskSnapshots_RecordedAt",
                table: "CustomerRiskSnapshots");

            migrationBuilder.AddColumn<decimal>(
                name: "BehaviorScore",
                table: "CustomerRiskSnapshots",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RiskScore",
                table: "CustomerRiskSnapshots",
                type: "numeric",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "idx_risk_snapshot_customer_date",
                table: "CustomerRiskSnapshots",
                columns: new[] { "CustomerId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_risk_snapshot_customer_date",
                table: "CustomerRiskSnapshots");

            migrationBuilder.DropColumn(
                name: "BehaviorScore",
                table: "CustomerRiskSnapshots");

            migrationBuilder.DropColumn(
                name: "RiskScore",
                table: "CustomerRiskSnapshots");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRiskSnapshots_CustomerId",
                table: "CustomerRiskSnapshots",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerRiskSnapshots_RecordedAt",
                table: "CustomerRiskSnapshots",
                column: "RecordedAt");
        }
    }
}
