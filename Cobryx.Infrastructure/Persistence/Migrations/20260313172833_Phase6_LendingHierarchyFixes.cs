
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_LendingHierarchyFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Credits_Customers_CustomerId",
                table: "Credits");

            migrationBuilder.DropForeignKey(
                name: "FK_Installments_Credits_CreditId",
                table: "Installments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Credits_CreditId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Loans_LoanId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "LedgerOutboxes");

            migrationBuilder.DropTable(
                name: "LoanInstallments");

            migrationBuilder.DropTable(
                name: "OutboxEvents");

            migrationBuilder.DropIndex(
                name: "IX_Users_TenantId_IsActive",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Payments_LoanId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_TenantId_CreatedAt",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Loans_CustomerId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_Status",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_TenantId",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_TenantId_IsDeleted",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_TenantId_RiskStatus",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Loans_TenantId_Status",
                table: "Loans");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_CreatedAt",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_TenantId_Status",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Credits_CustomerId",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Credits_StartDate",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Credits_Status",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Credits_TenantId",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Credits_TenantId_CreatedAt",
                table: "Credits");

            migrationBuilder.DropIndex(
                name: "IX_Credits_TenantId_Status",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "LoanId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "OriginalPrincipal",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "Frequency",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "GraceDays",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "InstallmentsCount",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "InterestRate",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "InterestType",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "InternalNotes",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "Principal_Amount",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "Principal_Currency",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Credits");

            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Loans",
                newName: "WriteOffDate");

            migrationBuilder.RenameColumn(
                name: "IsDeleted",
                table: "Loans",
                newName: "IsWrittenOff");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Loans",
                newName: "LastAccrualDate");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Installments",
                newName: "TotalAmount_Amount");

            migrationBuilder.RenameColumn(
                name: "RemainingBalance",
                table: "Installments",
                newName: "RemainingBalance_Amount");

            migrationBuilder.RenameColumn(
                name: "PrincipalPart",
                table: "Installments",
                newName: "PrincipalPart_Amount");

            migrationBuilder.RenameColumn(
                name: "PrincipalPaid",
                table: "Installments",
                newName: "PrincipalPaid_Amount");

            migrationBuilder.RenameColumn(
                name: "LateInterestPaid",
                table: "Installments",
                newName: "LateInterestPaid_Amount");

            migrationBuilder.RenameColumn(
                name: "LateInterestAmount",
                table: "Installments",
                newName: "LateInterestAmount_Amount");

            migrationBuilder.RenameColumn(
                name: "InterestPart",
                table: "Installments",
                newName: "InterestPart_Amount");

            migrationBuilder.RenameColumn(
                name: "InterestPaid",
                table: "Installments",
                newName: "InterestPaid_Amount");

            migrationBuilder.RenameColumn(
                name: "CreditId",
                table: "Installments",
                newName: "InstrumentId");

            migrationBuilder.RenameIndex(
                name: "IX_Installments_CreditId",
                table: "Installments",
                newName: "IX_Installments_InstrumentId");

            migrationBuilder.AddColumn<DateTime>(
                name: "DisbursementDate",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "PaymentApplicationPolicyId",
                table: "LoanAgreements",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<int>(
                name: "DayCountBasis",
                table: "InterestPolicies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "Installments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccruedCharges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,10)", precision: 18, scale: 10, nullable: false),
                    AccrualDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PostedToLedger = table.Column<bool>(type: "boolean", nullable: false),
                    LedgerSequenceId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_AccruedCharges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccruedCharges_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CollectionsPolicies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    EarlyStageDays = table.Column<int>(type: "integer", nullable: false),
                    ModerateStageDays = table.Column<int>(type: "integer", nullable: false),
                    SevereStageDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultStageDays = table.Column<int>(type: "integer", nullable: false),
                    WriteOffDays = table.Column<int>(type: "integer", nullable: false),
                    EnableLateFees = table.Column<bool>(type: "boolean", nullable: false),
                    EnableAutoWriteOff = table.Column<bool>(type: "boolean", nullable: false),
                    LateFeePolicyId = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("PK_CollectionsPolicies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CollectionsPolicies_LateFeePolicies_LateFeePolicyId",
                        column: x => x.LateFeePolicyId,
                        principalTable: "LateFeePolicies",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DeadLetterEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    FailedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_DeadLetterEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventShadowBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_EventShadowBalances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LendingInstruments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    Principal_Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Principal_Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    InterestRate = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InterestType = table.Column<string>(type: "text", nullable: false),
                    Frequency = table.Column<string>(type: "text", nullable: false),
                    InstallmentsCount = table.Column<int>(type: "integer", nullable: false),
                    GraceDays = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("PK_LendingInstruments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LendingInstruments_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanCollectionsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_LoanCollectionsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanCollectionsEvents_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanDelinquencyStates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    DaysPastDue = table.Column<int>(type: "integer", nullable: false),
                    OldestUnpaidDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    LastEvaluatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    LastEvaluatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_LoanDelinquencyStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanDelinquencyStates_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanPaymentAllocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrincipalApplied = table.Column<decimal>(type: "numeric", nullable: false),
                    InterestApplied = table.Column<decimal>(type: "numeric", nullable: false),
                    FeesApplied = table.Column<decimal>(type: "numeric", nullable: false),
                    UnappliedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SnapshotSequence = table.Column<long>(type: "bigint", nullable: false),
                    AllocationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_LoanPaymentAllocations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsProcessed = table.Column<bool>(type: "boolean", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    RetryCount = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    LedgerSequenceId = table.Column<long>(type: "bigint", nullable: true),
                    PartitionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
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
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProcessedEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsumerName = table.Column<string>(type: "text", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("PK_ProcessedEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ShadowBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric", nullable: false),
                    LastSequence = table.Column<long>(type: "bigint", nullable: false),
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
                    table.PrimaryKey("PK_ShadowBalances", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_InterestPolicyId",
                table: "LoanAgreements",
                column: "InterestPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_LateFeePolicyId",
                table: "LoanAgreements",
                column: "LateFeePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanAgreements_PaymentApplicationPolicyId",
                table: "LoanAgreements",
                column: "PaymentApplicationPolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceSnapshots_TenantId_AccountId_JournalSequenceId",
                table: "AccountBalanceSnapshots",
                columns: new[] { "TenantId", "AccountId", "JournalSequenceId" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "IX_AccruedCharges_LoanId_Type_AccrualDate",
                table: "AccruedCharges",
                columns: new[] { "LoanId", "Type", "AccrualDate" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CollectionsPolicies_LateFeePolicyId",
                table: "CollectionsPolicies",
                column: "LateFeePolicyId");

            migrationBuilder.CreateIndex(
                name: "IX_DeadLetterEvents_EventId",
                table: "DeadLetterEvents",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventShadowBalances_TenantId_AccountId",
                table: "EventShadowBalances",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.CreateIndex(
                name: "IX_LendingInstruments_CustomerId",
                table: "LendingInstruments",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_LendingInstruments_TenantId",
                table: "LendingInstruments",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_LendingInstruments_TenantId_CreatedAt",
                table: "LendingInstruments",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_LendingInstruments_TenantId_IsDeleted",
                table: "LendingInstruments",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanCollectionsEvents_LoanId",
                table: "LoanCollectionsEvents",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDelinquencyStates_DaysPastDue",
                table: "LoanDelinquencyStates",
                column: "DaysPastDue");

            migrationBuilder.CreateIndex(
                name: "IX_LoanDelinquencyStates_LoanId",
                table: "LoanDelinquencyStates",
                column: "LoanId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanDelinquencyStates_Stage",
                table: "LoanDelinquencyStates",
                column: "Stage");

            migrationBuilder.CreateIndex(
                name: "IX_LoanPaymentAllocations_PaymentId",
                table: "LoanPaymentAllocations",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CorrelationId",
                table: "OutboxMessages",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_EntityId",
                table: "OutboxMessages",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_GenericQueue",
                table: "OutboxMessages",
                columns: new[] { "IsProcessed", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_LedgerQueue",
                table: "OutboxMessages",
                columns: new[] { "IsProcessed", "LedgerSequenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedEvents_EventId_ConsumerName",
                table: "ProcessedEvents",
                columns: new[] { "EventId", "ConsumerName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShadowBalances_LastSequence",
                table: "ShadowBalances",
                column: "LastSequence");

            migrationBuilder.CreateIndex(
                name: "IX_ShadowBalances_TenantId_AccountId",
                table: "ShadowBalances",
                columns: new[] { "TenantId", "AccountId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Credits_LendingInstruments_Id",
                table: "Credits",
                column: "Id",
                principalTable: "LendingInstruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Installments_LendingInstruments_InstrumentId",
                table: "Installments",
                column: "InstrumentId",
                principalTable: "LendingInstruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanAgreements_InterestPolicies_InterestPolicyId",
                table: "LoanAgreements",
                column: "InterestPolicyId",
                principalTable: "InterestPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_LoanAgreements_LateFeePolicies_LateFeePolicyId",
                table: "LoanAgreements",
                column: "LateFeePolicyId",
                principalTable: "LateFeePolicies",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_LoanAgreements_PaymentApplicationPolicies_PaymentApplicatio~",
                table: "LoanAgreements",
                column: "PaymentApplicationPolicyId",
                principalTable: "PaymentApplicationPolicies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Loans_LendingInstruments_Id",
                table: "Loans",
                column: "Id",
                principalTable: "LendingInstruments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Credits_CreditId",
                table: "Payments",
                column: "CreditId",
                principalTable: "Credits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Credits_LendingInstruments_Id",
                table: "Credits");

            migrationBuilder.DropForeignKey(
                name: "FK_Installments_LendingInstruments_InstrumentId",
                table: "Installments");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanAgreements_InterestPolicies_InterestPolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanAgreements_LateFeePolicies_LateFeePolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_LoanAgreements_PaymentApplicationPolicies_PaymentApplicatio~",
                table: "LoanAgreements");

            migrationBuilder.DropForeignKey(
                name: "FK_Loans_LendingInstruments_Id",
                table: "Loans");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Credits_CreditId",
                table: "Payments");

            migrationBuilder.DropTable(
                name: "AccruedCharges");

            migrationBuilder.DropTable(
                name: "CollectionsPolicies");

            migrationBuilder.DropTable(
                name: "DeadLetterEvents");

            migrationBuilder.DropTable(
                name: "EventShadowBalances");

            migrationBuilder.DropTable(
                name: "LendingInstruments");

            migrationBuilder.DropTable(
                name: "LoanCollectionsEvents");

            migrationBuilder.DropTable(
                name: "LoanDelinquencyStates");

            migrationBuilder.DropTable(
                name: "LoanPaymentAllocations");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProcessedEvents");

            migrationBuilder.DropTable(
                name: "ShadowBalances");

            migrationBuilder.DropIndex(
                name: "IX_LoanAgreements_InterestPolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropIndex(
                name: "IX_LoanAgreements_LateFeePolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropIndex(
                name: "IX_LoanAgreements_PaymentApplicationPolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropIndex(
                name: "IX_AccountBalanceSnapshots_TenantId_AccountId_JournalSequenceId",
                table: "AccountBalanceSnapshots");

            migrationBuilder.DropColumn(
                name: "DisbursementDate",
                table: "Loans");

            migrationBuilder.DropColumn(
                name: "PaymentApplicationPolicyId",
                table: "LoanAgreements");

            migrationBuilder.DropColumn(
                name: "DayCountBasis",
                table: "InterestPolicies");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "Installments");

            migrationBuilder.RenameColumn(
                name: "WriteOffDate",
                table: "Loans",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "LastAccrualDate",
                table: "Loans",
                newName: "CreatedAt");

            migrationBuilder.RenameColumn(
                name: "IsWrittenOff",
                table: "Loans",
                newName: "IsDeleted");

            migrationBuilder.RenameColumn(
                name: "TotalAmount_Amount",
                table: "Installments",
                newName: "TotalAmount");

            migrationBuilder.RenameColumn(
                name: "RemainingBalance_Amount",
                table: "Installments",
                newName: "RemainingBalance");

            migrationBuilder.RenameColumn(
                name: "PrincipalPart_Amount",
                table: "Installments",
                newName: "PrincipalPart");

            migrationBuilder.RenameColumn(
                name: "PrincipalPaid_Amount",
                table: "Installments",
                newName: "PrincipalPaid");

            migrationBuilder.RenameColumn(
                name: "LateInterestPaid_Amount",
                table: "Installments",
                newName: "LateInterestPaid");

            migrationBuilder.RenameColumn(
                name: "LateInterestAmount_Amount",
                table: "Installments",
                newName: "LateInterestAmount");

            migrationBuilder.RenameColumn(
                name: "InterestPart_Amount",
                table: "Installments",
                newName: "InterestPart");

            migrationBuilder.RenameColumn(
                name: "InterestPaid_Amount",
                table: "Installments",
                newName: "InterestPaid");

            migrationBuilder.RenameColumn(
                name: "InstrumentId",
                table: "Installments",
                newName: "CreditId");

            migrationBuilder.RenameIndex(
                name: "IX_Installments_InstrumentId",
                table: "Installments",
                newName: "IX_Installments_CreditId");

            migrationBuilder.AddColumn<Guid>(
                name: "LoanId",
                table: "Payments",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Loans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Loans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Loans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Loans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Loans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OriginalPrincipal",
                table: "Loans",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Loans",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Loans",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Loans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Loans",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "Credits",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Credits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CustomerId",
                table: "Credits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "Credits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Frequency",
                table: "Credits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GraceDays",
                table: "Credits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentsCount",
                table: "Credits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "InterestRate",
                table: "Credits",
                type: "numeric(18,4)",
                precision: 18,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "InterestType",
                table: "Credits",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "InternalNotes",
                table: "Credits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Credits",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Credits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Principal_Amount",
                table: "Credits",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Principal_Currency",
                table: "Credits",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Tags",
                table: "Credits",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TenantId",
                table: "Credits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "Credits",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UpdatedBy",
                table: "Credits",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "Version",
                table: "Credits",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.CreateTable(
                name: "LedgerOutboxes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    JournalSequenceId = table.Column<long>(type: "bigint", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LedgerOutboxes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LoanInstallments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    LoanId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    InstallmentNumber = table.Column<int>(type: "integer", nullable: false),
                    InterestAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InterestPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    LateFeeAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    LateFeePaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PrincipalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PrincipalPaid = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanInstallments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanInstallments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    CorrelationId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Error = table.Column<string>(type: "text", nullable: true),
                    InternalNotes = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    OccurredOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Tags = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_TenantId_IsActive",
                table: "Users",
                columns: new[] { "TenantId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_LoanId",
                table: "Payments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_TenantId_CreatedAt",
                table: "Payments",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_CustomerId",
                table: "Loans",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_Status",
                table: "Loans",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId",
                table: "Loans",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId_IsDeleted",
                table: "Loans",
                columns: new[] { "TenantId", "IsDeleted" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId_RiskStatus",
                table: "Loans",
                columns: new[] { "TenantId", "RiskStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_Loans_TenantId_Status",
                table: "Loans",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_CreatedAt",
                table: "Invoices",
                columns: new[] { "TenantId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_TenantId_Status",
                table: "Invoices",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Credits_CustomerId",
                table: "Credits",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_StartDate",
                table: "Credits",
                column: "StartDate");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_Status",
                table: "Credits",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_TenantId",
                table: "Credits",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Credits_TenantId_CreatedAt",
                table: "Credits",
                columns: new[] { "TenantId", "CreatedAt" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "IX_Credits_TenantId_Status",
                table: "Credits",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_DueDate",
                table: "LoanInstallments",
                column: "DueDate");

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_LoanId",
                table: "LoanInstallments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_LoanId_InstallmentNumber",
                table: "LoanInstallments",
                columns: new[] { "LoanId", "InstallmentNumber" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanInstallments_Status",
                table: "LoanInstallments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_OccurredOnUtc",
                table: "OutboxEvents",
                column: "OccurredOnUtc");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Processed_Occurred",
                table: "OutboxEvents",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_ProcessedOnUtc",
                table: "OutboxEvents",
                column: "ProcessedOnUtc");

            migrationBuilder.AddForeignKey(
                name: "FK_Credits_Customers_CustomerId",
                table: "Credits",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Installments_Credits_CreditId",
                table: "Installments",
                column: "CreditId",
                principalTable: "Credits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Credits_CreditId",
                table: "Payments",
                column: "CreditId",
                principalTable: "Credits",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Loans_LoanId",
                table: "Payments",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "Id");
        }
    }
}
