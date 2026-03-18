using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionsCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "LoanBalanceSnapshots",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "CollectionActions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActionType = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ExecutedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: false),
                    Outstanding = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    AssignedAgentId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastContactedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextActionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false),
                    PriorityScore = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionCases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CollectionPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReminderDays = table.Column<int>(type: "integer", nullable: false),
                    CallDays = table.Column<int>(type: "integer", nullable: false),
                    EscalationDays = table.Column<int>(type: "integer", nullable: false),
                    LegalDays = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CollectionPolicies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LatestLoanSnapshots",
                columns: table => new
                {
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    LateFeeBalance = table.Column<decimal>(type: "numeric", nullable: false),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "TenantPortfolioAggregates",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalLoans = table.Column<int>(type: "integer", nullable: false),
                    TotalOutstanding = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalPrincipal = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalInterest = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalLateFees = table.Column<decimal>(type: "numeric", nullable: false),
                    NplOutstanding = table.Column<decimal>(type: "numeric", nullable: false),
                    Bucket0To30 = table.Column<decimal>(type: "numeric", nullable: false),
                    Bucket31To60 = table.Column<decimal>(type: "numeric", nullable: false),
                    Bucket61To90 = table.Column<decimal>(type: "numeric", nullable: false),
                    Bucket90Plus = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionActions_CaseId",
                table: "CollectionActions",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCases_LoanId",
                table: "CollectionCases",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionCases_TenantId_IsClosed_PriorityScore",
                table: "CollectionCases",
                columns: new[] { "TenantId", "IsClosed", "PriorityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_CollectionPolicies_TenantId",
                table: "CollectionPolicies",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CollectionActions");

            migrationBuilder.DropTable(
                name: "CollectionCases");

            migrationBuilder.DropTable(
                name: "CollectionPolicies");

            migrationBuilder.DropTable(
                name: "LatestLoanSnapshots");

            migrationBuilder.DropTable(
                name: "TenantPortfolioAggregates");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "LoanBalanceSnapshots");
        }
    }
}
