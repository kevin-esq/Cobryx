using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDeterministicAuditTrail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Experiences",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "DecisionDistributionLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreditMultipliers = table.Column<decimal[]>(type: "numeric[]", nullable: false),
                    InterestDeltas = table.Column<decimal[]>(type: "numeric[]", nullable: false),
                    VaR95 = table.Column<decimal>(type: "numeric", nullable: false),
                    CVaR95 = table.Column<decimal>(type: "numeric", nullable: false),
                    ScenarioSetJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DecisionDistributionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReplaySnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FeatureVectorJson = table.Column<string>(type: "text", nullable: false),
                    MacroStateJson = table.Column<string>(type: "text", nullable: false),
                    PortfolioStateJson = table.Column<string>(type: "text", nullable: false),
                    DecisionContextJson = table.Column<string>(type: "text", nullable: false),
                    ExecutionTraceJson = table.Column<string>(type: "text", nullable: false),
                    EngineVersion = table.Column<string>(type: "text", nullable: false),
                    ConfigHash = table.Column<string>(type: "text", nullable: false),
                    OriginalCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    OriginalInterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    ReplayedCreditLimit = table.Column<decimal>(type: "numeric", nullable: true),
                    ReplayedInterestRate = table.Column<decimal>(type: "numeric", nullable: true),
                    ModelVersion = table.Column<string>(type: "text", nullable: false),
                    ReplayModelVersion = table.Column<string>(type: "text", nullable: false),
                    DeltaCredit = table.Column<decimal>(type: "numeric", nullable: false),
                    DeltaInterest = table.Column<decimal>(type: "numeric", nullable: false),
                    RandomSeed = table.Column<int>(type: "integer", nullable: false),
                    ModelHash = table.Column<string>(type: "text", nullable: false),
                    FeatureVersion = table.Column<string>(type: "text", nullable: false),
                    ScenarioVersion = table.Column<string>(type: "text", nullable: false),
                    RealOutcomeDefaulted = table.Column<bool>(type: "boolean", nullable: true),
                    RealOutcomeRecovered = table.Column<decimal>(type: "numeric", nullable: true),
                    RealOutcomeProfit = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReplaySnapshots", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DecisionDistributionLogs");

            migrationBuilder.DropTable(
                name: "ReplaySnapshots");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Experiences");
        }
    }
}
