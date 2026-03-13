using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStripeEventIdempotencyForConnect : Migration
    {
        private static readonly string[] columns = new[] { "StripeEventId", "StripeAccountId" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedStripeEvents_StripeEventId",
                table: "ProcessedStripeEvents");

            migrationBuilder.AddColumn<string>(
                name: "StripeAccountId",
                table: "ProcessedStripeEvents",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedStripeEvents_StripeEventId_StripeAccountId",
                table: "ProcessedStripeEvents",
                columns: columns,
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedStripeEvents_StripeEventId_StripeAccountId",
                table: "ProcessedStripeEvents");

            migrationBuilder.DropColumn(
                name: "StripeAccountId",
                table: "ProcessedStripeEvents");

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedStripeEvents_StripeEventId",
                table: "ProcessedStripeEvents",
                column: "StripeEventId",
                unique: true);
        }
    }
}
