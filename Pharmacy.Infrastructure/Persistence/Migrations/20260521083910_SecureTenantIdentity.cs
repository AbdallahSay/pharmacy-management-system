using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharmacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SecureTenantIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE users
                SET TenantId = tenantUsers.TenantId
                FROM AspNetUsers AS users
                INNER JOIN (
                    SELECT UserId, MIN(TenantId) AS TenantId
                    FROM TenantUsers
                    GROUP BY UserId
                ) AS tenantUsers ON users.Id = tenantUsers.UserId;
                """);

            migrationBuilder.Sql("""
                UPDATE users
                SET TenantId = tenants.Id
                FROM AspNetUsers AS users
                CROSS JOIN (
                    SELECT TOP 1 Id
                    FROM Tenants
                    ORDER BY Id
                ) AS tenants
                WHERE users.TenantId = 0;
                """);

            migrationBuilder.Sql("""
                UPDATE TenantUsers
                SET Role = 'TenantAdmin'
                WHERE Role = 'Admin';
                """);

            migrationBuilder.Sql("""
                DECLARE @AdminRoleId int = (
                    SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = 'ADMIN'
                );
                DECLARE @PlatformRoleId int = (
                    SELECT TOP 1 Id FROM AspNetRoles WHERE NormalizedName = 'PLATFORMADMIN'
                );

                IF @AdminRoleId IS NOT NULL AND @PlatformRoleId IS NULL
                BEGIN
                    UPDATE AspNetRoles
                    SET Name = 'PlatformAdmin',
                        NormalizedName = 'PLATFORMADMIN'
                    WHERE Id = @AdminRoleId;
                END
                ELSE IF @AdminRoleId IS NOT NULL AND @PlatformRoleId IS NOT NULL
                BEGIN
                    DELETE userRoles
                    FROM AspNetUserRoles AS userRoles
                    WHERE userRoles.RoleId = @AdminRoleId
                      AND EXISTS (
                          SELECT 1
                          FROM AspNetUserRoles AS existing
                          WHERE existing.UserId = userRoles.UserId
                            AND existing.RoleId = @PlatformRoleId
                      );

                    UPDATE AspNetUserRoles
                    SET RoleId = @PlatformRoleId
                    WHERE RoleId = @AdminRoleId;

                    DELETE FROM AspNetRoles
                    WHERE Id = @AdminRoleId;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (SELECT 1 FROM AspNetUsers WHERE TenantId = 0)
                BEGIN
                    THROW 51000, 'Cannot apply SecureTenantIdentity: every existing user must be linked to a tenant.', 1;
                END
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedEmail",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "NormalizedEmail" },
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedUserName",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "NormalizedUserName" },
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedEmail",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId_NormalizedUserName",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);
        }
    }
}
