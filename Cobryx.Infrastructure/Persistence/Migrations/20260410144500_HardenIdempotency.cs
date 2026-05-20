using Microsoft.EntityFrameworkCore.Migrations;

# nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HardenIdempotency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ResourceType",
                table: "idempotency_records",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ResourceId",
                table: "idempotency_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "idempotency_records",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "idempotency_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CausationId",
                table: "idempotency_records",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ResourceType",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "ResourceId",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "idempotency_records");

            migrationBuilder.DropColumn(
                name: "CausationId",
                table: "idempotency_records");
        }
    }
}
