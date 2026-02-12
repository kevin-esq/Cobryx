using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Cobryx.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRbacV2AndMonetization : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionsId",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RolesId",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_RolesId",
                table: "RolePermissions");

            // User and Subscription enhancements
            migrationBuilder.AddColumn<int>(
                name: "PermissionVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "GracePeriodEndsAtUtc",
                table: "TenantSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            // RolePermissions - Add new column first
            migrationBuilder.AddColumn<string>(
                name: "PermissionKey",
                table: "RolePermissions",
                type: "character varying(50)",
                nullable: false,
                defaultValue: "");

            // Migrate data: Map PermissionsId (Guid) to PermissionKey (Name/String)
            migrationBuilder.Sql(@"
                UPDATE ""RolePermissions"" rp
                SET ""PermissionKey"" = p.""Name""
                FROM ""Permissions"" p
                WHERE rp.""PermissionsId"" = p.""Id""");

            // Now clean up the old schema
            migrationBuilder.DropColumn(
                name: "PermissionsId",
                table: "RolePermissions");

            migrationBuilder.RenameColumn(
                name: "RolesId",
                table: "RolePermissions",
                newName: "RoleId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionKey" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Permissions_Name",
                table: "Permissions",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionKey",
                table: "RolePermissions",
                column: "PermissionKey");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxEvents_Processed_Occurred",
                table: "OutboxEvents",
                columns: new[] { "ProcessedOnUtc", "OccurredOnUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionKey",
                table: "RolePermissions",
                column: "PermissionKey",
                principalTable: "Permissions",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionKey",
                table: "RolePermissions");

            migrationBuilder.DropForeignKey(
                name: "FK_RolePermissions_Roles_RoleId",
                table: "RolePermissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions");

            migrationBuilder.DropIndex(
                name: "IX_RolePermissions_PermissionKey",
                table: "RolePermissions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Permissions_Name",
                table: "Permissions");

            migrationBuilder.DropIndex(
                name: "IX_OutboxEvents_Processed_Occurred",
                table: "OutboxEvents");

            migrationBuilder.DropColumn(
                name: "PermissionVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "TenantSubscriptions");

            migrationBuilder.DropColumn(
                name: "GracePeriodEndsAtUtc",
                table: "TenantSubscriptions");

            migrationBuilder.AddColumn<Guid>(
                name: "PermissionsId",
                table: "RolePermissions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            // Migrate data back: Map PermissionKey (Name/String) to PermissionsId (Guid)
            migrationBuilder.Sql(@"
                UPDATE ""RolePermissions"" rp
                SET ""PermissionsId"" = p.""Id""
                FROM ""Permissions"" p
                WHERE rp.""PermissionKey"" = p.""Name""");

            migrationBuilder.DropColumn(
                name: "PermissionKey",
                table: "RolePermissions");

            migrationBuilder.RenameColumn(
                name: "RoleId",
                table: "RolePermissions",
                newName: "RolesId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_RolePermissions",
                table: "RolePermissions",
                columns: new[] { "PermissionsId", "RolesId" });

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_RolesId",
                table: "RolePermissions",
                column: "RolesId");

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Permissions_PermissionsId",
                table: "RolePermissions",
                column: "PermissionsId",
                principalTable: "Permissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_RolePermissions_Roles_RolesId",
                table: "RolePermissions",
                column: "RolesId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
