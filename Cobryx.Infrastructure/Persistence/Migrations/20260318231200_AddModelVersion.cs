using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModelVersion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "ModelOutcomes",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ModelVersion",
                table: "DecisionSnapshots",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "ModelOutcomes");

            migrationBuilder.DropColumn(
                name: "ModelVersion",
                table: "DecisionSnapshots");
        }
    }
}
