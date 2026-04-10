using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIdempotencyRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ResponseBody = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: true),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    LocationHeader = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExternalGatewayId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProductionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HashedCustomerId = table.Column<string>(type: "text", nullable: false),
                    EngineVersion = table.Column<string>(type: "text", nullable: false),
                    ConfigHash = table.Column<string>(type: "text", nullable: false),
                    TraceHash = table.Column<string>(type: "text", nullable: false),
                    IsFullSnapshot = table.Column<bool>(type: "boolean", nullable: false),
                    FeatureVectorJson = table.Column<string>(type: "text", nullable: true),
                    MacroStateJson = table.Column<string>(type: "text", nullable: true),
                    PortfolioStateJson = table.Column<string>(type: "text", nullable: true),
                    ExecutionTraceJson = table.Column<string>(type: "text", nullable: true),
                    DecisionResultJson = table.Column<string>(type: "text", nullable: true),
                    ResultCreditLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    ResultInterestRate = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductionSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegressionReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalProcessed = table.Column<int>(type: "integer", nullable: false),
                    PassedCount = table.Column<int>(type: "integer", nullable: false),
                    FailedCount = table.Column<int>(type: "integer", nullable: false),
                    NonComparableCount = table.Column<int>(type: "integer", nullable: false),
                    MeanLimitDrift = table.Column<decimal>(type: "numeric", nullable: false),
                    P95LimitDrift = table.Column<decimal>(type: "numeric", nullable: false),
                    MaxLimitDrift = table.Column<decimal>(type: "numeric", nullable: false),
                    TargetEngineVersion = table.Column<string>(type: "text", nullable: false),
                    BaselineEngineVersion = table.Column<string>(type: "text", nullable: true),
                    ParentEngineVersion = table.Column<string>(type: "text", nullable: true),
                    SampleRate = table.Column<int>(type: "integer", nullable: false),
                    SampleSize = table.Column<int>(type: "integer", nullable: false),
                    DatasetHash = table.Column<string>(type: "text", nullable: true),
                    TopFailuresJson = table.Column<string>(type: "text", nullable: false),
                    SeverityDistributionJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegressionReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShadowDriftEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "uuid", nullable: false),
                    EngineVersion = table.Column<string>(type: "text", nullable: false),
                    ShadowVersion = table.Column<string>(type: "text", nullable: false),
                    DeltaLimit = table.Column<decimal>(type: "numeric", nullable: false),
                    DeltaRate = table.Column<decimal>(type: "numeric", nullable: false),
                    OutputSeverity = table.Column<int>(type: "integer", nullable: false),
                    TraceSeverity = table.Column<int>(type: "integer", nullable: false),
                    AttributionJson = table.Column<string>(type: "text", nullable: true),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShadowDriftEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_expires_at",
                table: "idempotency_records",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_gateway_id",
                table: "idempotency_records",
                column: "ExternalGatewayId",
                filter: "\"ExternalGatewayId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_records_tenant_key",
                table: "idempotency_records",
                columns: new[] { "TenantId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSnapshots_ConfigHash",
                table: "ProductionSnapshots",
                column: "ConfigHash");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSnapshots_CreatedAt",
                table: "ProductionSnapshots",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSnapshots_EngineVersion",
                table: "ProductionSnapshots",
                column: "EngineVersion");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSnapshots_HashedCustomerId",
                table: "ProductionSnapshots",
                column: "HashedCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductionSnapshots_TraceHash",
                table: "ProductionSnapshots",
                column: "TraceHash");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "idempotency_records");

            migrationBuilder.DropTable(
                name: "ProductionSnapshots");

            migrationBuilder.DropTable(
                name: "RegressionReports");

            migrationBuilder.DropTable(
                name: "ShadowDriftEvents");
        }
    }
}
