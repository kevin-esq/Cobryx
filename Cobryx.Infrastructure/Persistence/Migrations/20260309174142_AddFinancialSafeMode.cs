using Microsoft.EntityFrameworkCore.Migrations;

using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialSafeMode : Migration
    {
        private static readonly string[] columns = new[] { "TenantId", "Id" };

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_TenantId",
                table: "LedgerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_AccountId",
                table: "LedgerEntries");

            migrationBuilder.AddColumn<bool>(
                name: "FinancialSafeMode",
                table: "Tenants",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SuspendedAt",
                table: "Tenants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LedgerFingerprintSnapshot",
                table: "ReconciliationAudits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "LedgerTransactions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "LedgerEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "JournalSequenceId",
                table: "LedgerEntries",
                type: "bigint",
                nullable: false,
                defaultValue: 0L)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceId",
                table: "LedgerEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "LedgerAccounts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AccountBalanceSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalSequenceId = table.Column<long>(type: "bigint", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("PK_AccountBalanceSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BankMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "text", nullable: false),
                    Direction = table.Column<string>(type: "text", nullable: false),
                    BookingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ValueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ProviderTransactionId = table.Column<string>(type: "text", nullable: false),
                    ExternalRef = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: false),
                    MatchedLedgerTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    MatchingConfidence = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: true),
                    MatchingType = table.Column<string>(type: "text", nullable: true),
                    RawPayload = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_BankMovements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalCheckpoints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastProcessedEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    LastProcessedSequenceId = table.Column<long>(type: "bigint", nullable: false),
                    LastEntryCreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastFingerprint = table.Column<string>(type: "text", nullable: false),
                    EntryCount = table.Column<int>(type: "integer", nullable: false),
                    HashVersion = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_JournalCheckpoints", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LedgerOutboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalSequenceId = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_LedgerOutboxes", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_TenantId_Id",
                table: "LedgerTransactions",
                columns: columns);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_TenantId_IsReversal",
                table: "LedgerTransactions",
                columns: new[] { "TenantId", "IsReversal" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_TenantId_LoanId",
                table: "LedgerTransactions",
                columns: new[] { "TenantId", "LoanId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_JournalSequenceId",
                table: "LedgerEntries",
                column: "JournalSequenceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_AccountId_CreatedAt",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_AccountId_JournalSequenceId",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "AccountId", "JournalSequenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_CreatedAt",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_Id",
                table: "LedgerEntries",
                columns: columns);

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_TenantId_JournalSequenceId",
                table: "LedgerEntries",
                columns: new[] { "TenantId", "JournalSequenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_BankMovements_Status",
                table: "BankMovements",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_BankMovements_TenantId_BookingDate",
                table: "BankMovements",
                columns: new[] { "TenantId", "BookingDate" });

            migrationBuilder.CreateIndex(
                name: "IX_BankMovements_TenantId_ProviderTransactionId",
                table: "BankMovements",
                columns: new[] { "TenantId", "ProviderTransactionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JournalCheckpoints_TenantId",
                table: "JournalCheckpoints",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalanceSnapshots");

            migrationBuilder.DropTable(
                name: "BankMovements");

            migrationBuilder.DropTable(
                name: "JournalCheckpoints");

            migrationBuilder.DropTable(
                name: "LedgerOutboxes");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_TenantId_Id",
                table: "LedgerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_TenantId_IsReversal",
                table: "LedgerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LedgerTransactions_TenantId_LoanId",
                table: "LedgerTransactions");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_JournalSequenceId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId_AccountId_CreatedAt",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId_AccountId_JournalSequenceId",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId_CreatedAt",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId_Id",
                table: "LedgerEntries");

            migrationBuilder.DropIndex(
                name: "IX_LedgerEntries_TenantId_JournalSequenceId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "FinancialSafeMode",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "SuspendedAt",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "LedgerFingerprintSnapshot",
                table: "ReconciliationAudits");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "LedgerTransactions");

            migrationBuilder.DropColumn(
                name: "Currency",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "JournalSequenceId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "ReferenceId",
                table: "LedgerEntries");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "LedgerAccounts");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerTransactions_TenantId",
                table: "LedgerTransactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LedgerEntries_AccountId",
                table: "LedgerEntries",
                column: "AccountId");
        }
    }
}
