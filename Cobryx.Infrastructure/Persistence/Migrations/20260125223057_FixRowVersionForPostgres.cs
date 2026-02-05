using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixRowVersionForPostgres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SystemErrorLogs");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SupportTickets");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Permissions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Installments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Credits");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "AuditLogs");

            // RefreshTokens handled separately if needed, but keeping existing logic if appropriate or dropping if it was RowVersion.
            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "RefreshTokens");

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Users",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Tenants",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "SystemErrorLogs",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "SupportTickets",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Roles",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Products",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Permissions",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Payments",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Installments",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Customers",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "Credits",
                newName: "RowVersion");

            migrationBuilder.RenameColumn(
                name: "xmin",
                table: "AuditLogs",
                newName: "RowVersion");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Users",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Tenants",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "SystemErrorLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "SupportTickets",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Roles",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "RefreshTokens",
                type: "bytea",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Products",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Permissions",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Payments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Installments",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Customers",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "Credits",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);

            migrationBuilder.AlterColumn<byte[]>(
                name: "RowVersion",
                table: "AuditLogs",
                type: "bytea",
                rowVersion: true,
                nullable: false,
                oldClrType: typeof(uint),
                oldType: "xid",
                oldRowVersion: true);
        }
    }
}
