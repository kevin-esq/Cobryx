using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionOutcomes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CollectionOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WasSuccessful = table.Column<bool>(type: "boolean", nullable: false),
                    AmountRecovered = table.Column<decimal>(type: "numeric(18,4)", nullable: false),
                    DaysToRecover = table.Column<int>(type: "integer", nullable: false),
                    DaysPastDueAtAction = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionOutcomes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionOutcomes_TenantId_DaysPastDueAtAction",
                table: "CollectionOutcomes",
                columns: new[] { "TenantId", "DaysPastDueAtAction" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionOutcomes");
        }
    }
}
