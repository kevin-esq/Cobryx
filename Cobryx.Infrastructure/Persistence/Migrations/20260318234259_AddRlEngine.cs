#nullable disable

using Microsoft.EntityFrameworkCore.Migrations;

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRlEngine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DecisionOutcomes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    StateKey = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    CreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    Defaulted = table.Column<bool>(type: "boolean", nullable: false),
                    AmountRecovered = table.Column<decimal>(type: "numeric", nullable: false),
                    Reward = table.Column<decimal>(type: "numeric", nullable: false),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionOutcomes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MlModels",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<string>(type: "text", nullable: false),
                    IsProduction = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MlModels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StateKey = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    Value = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QValues", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShadowPredictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductionPd = table.Column<decimal>(type: "numeric", nullable: false),
                    ShadowPd = table.Column<decimal>(type: "numeric", nullable: false),
                    ProductionModelVersion = table.Column<string>(type: "text", nullable: false),
                    ShadowModelVersion = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShadowPredictions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionOutcomes");

            migrationBuilder.DropTable(
                name: "MlModels");

            migrationBuilder.DropTable(
                name: "QValues");

            migrationBuilder.DropTable(
                name: "ShadowPredictions");
        }
    }
}
