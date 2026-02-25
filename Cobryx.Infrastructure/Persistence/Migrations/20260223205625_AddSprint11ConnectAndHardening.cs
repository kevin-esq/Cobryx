using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSprint11ConnectAndHardening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Connect_ChargesEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Connect_DetailsSubmitted",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Connect_PayoutsEnabled",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastRecoveryAttemptAt",
                table: "PaymentLinks",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RecoveryAttemptCount",
                table: "PaymentLinks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_Status",
                table: "PaymentLinks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentLinks_Status_UpdatedAt",
                table: "PaymentLinks",
                columns: new[] { "Status", "UpdatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PaymentLinks_Status",
                table: "PaymentLinks");

            migrationBuilder.DropIndex(
                name: "IX_PaymentLinks_Status_UpdatedAt",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "Connect_ChargesEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Connect_DetailsSubmitted",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "Connect_PayoutsEnabled",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LastRecoveryAttemptAt",
                table: "PaymentLinks");

            migrationBuilder.DropColumn(
                name: "RecoveryAttemptCount",
                table: "PaymentLinks");
        }
    }
}
