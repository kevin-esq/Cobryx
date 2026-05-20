using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessSettingsHighRiskLedgerThreshold : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE "Tenants"
                    ADD COLUMN IF NOT EXISTS "Settings_HighRiskLedgerThreshold" numeric NOT NULL DEFAULT 10000.0;

                ALTER TABLE "LedgerTransactions"
                    ADD COLUMN IF NOT EXISTS "CausationId" uuid NULL;

                ALTER TABLE "LedgerTransactions"
                    ADD COLUMN IF NOT EXISTS "CorrelationId" uuid NULL;

                ALTER TABLE "LedgerTransactions"
                    ADD COLUMN IF NOT EXISTS "Hash" char(64) NULL;

                ALTER TABLE "LedgerTransactions"
                    ADD COLUMN IF NOT EXISTS "PreviousHash" char(64) NULL;

                ALTER TABLE "LedgerTransactions"
                    ADD COLUMN IF NOT EXISTS "Sequence" bigint NOT NULL DEFAULT 0;

                WITH ranked AS (
                    SELECT
                        "Id",
                        ROW_NUMBER() OVER (
                            PARTITION BY "TenantId"
                            ORDER BY "CreatedAt", "Id"
                        ) AS "NextSequence"
                    FROM "LedgerTransactions"
                    WHERE "Sequence" = 0
                )
                UPDATE "LedgerTransactions" AS lt
                SET "Sequence" = ranked."NextSequence"
                FROM ranked
                WHERE lt."Id" = ranked."Id";

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_LedgerTransactions_TenantId_Sequence"
                    ON "LedgerTransactions" ("TenantId", "Sequence");

                CREATE UNIQUE INDEX IF NOT EXISTS "IX_LedgerTransactions_TenantId_Hash"
                    ON "LedgerTransactions" ("TenantId", "Hash");
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "IX_LedgerTransactions_TenantId_Hash";
                DROP INDEX IF EXISTS "IX_LedgerTransactions_TenantId_Sequence";

                ALTER TABLE "Tenants"
                    DROP COLUMN IF EXISTS "Settings_HighRiskLedgerThreshold";

                ALTER TABLE "LedgerTransactions"
                    DROP COLUMN IF EXISTS "CausationId";

                ALTER TABLE "LedgerTransactions"
                    DROP COLUMN IF EXISTS "CorrelationId";

                ALTER TABLE "LedgerTransactions"
                    DROP COLUMN IF EXISTS "Hash";

                ALTER TABLE "LedgerTransactions"
                    DROP COLUMN IF EXISTS "PreviousHash";

                ALTER TABLE "LedgerTransactions"
                    DROP COLUMN IF EXISTS "Sequence";
                """);
        }
    }
}
