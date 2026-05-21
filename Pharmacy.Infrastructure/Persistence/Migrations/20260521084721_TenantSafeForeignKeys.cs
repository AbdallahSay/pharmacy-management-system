using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Pharmacy.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class TenantSafeForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM Medicines AS medicines
                    LEFT JOIN Categories AS categories
                        ON categories.Id = medicines.CategoryId
                       AND categories.TenantId = medicines.TenantId
                    WHERE categories.Id IS NULL
                )
                BEGIN
                    THROW 51001, 'Cannot apply TenantSafeForeignKeys: Medicine references a Category from another tenant.', 1;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM SaleItems AS saleItems
                    LEFT JOIN Sales AS sales
                        ON sales.Id = saleItems.SaleId
                       AND sales.TenantId = saleItems.TenantId
                    WHERE sales.Id IS NULL
                )
                BEGIN
                    THROW 51002, 'Cannot apply TenantSafeForeignKeys: SaleItem references a Sale from another tenant.', 1;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM SaleItems AS saleItems
                    LEFT JOIN Medicines AS medicines
                        ON medicines.Id = saleItems.MedicineId
                       AND medicines.TenantId = saleItems.TenantId
                    WHERE medicines.Id IS NULL
                )
                BEGIN
                    THROW 51003, 'Cannot apply TenantSafeForeignKeys: SaleItem references a Medicine from another tenant.', 1;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM Sales AS sales
                    LEFT JOIN AspNetUsers AS users
                        ON users.Id = sales.UserId
                       AND users.TenantId = sales.TenantId
                    WHERE users.Id IS NULL
                )
                BEGIN
                    THROW 51004, 'Cannot apply TenantSafeForeignKeys: Sale references a User from another tenant.', 1;
                END
                """);

            migrationBuilder.Sql("""
                IF EXISTS (
                    SELECT 1
                    FROM TenantUsers AS tenantUsers
                    LEFT JOIN AspNetUsers AS users
                        ON users.Id = tenantUsers.UserId
                       AND users.TenantId = tenantUsers.TenantId
                    WHERE users.Id IS NULL
                )
                BEGIN
                    THROW 51005, 'Cannot apply TenantSafeForeignKeys: TenantUser references a User from another tenant.', 1;
                END
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Medicines_MedicineId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Sales_SaleId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_UserId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_TenantUsers_UserId",
                table: "TenantUsers");

            migrationBuilder.DropIndex(
                name: "IX_Sales_UserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_MedicineId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines");

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                table: "Sales",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Medicines_TenantId_Id",
                table: "Medicines",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_Categories_TenantId_Id",
                table: "Categories",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_AspNetUsers_TenantId_Id",
                table: "AspNetUsers",
                columns: new[] { "TenantId", "Id" });

            migrationBuilder.CreateIndex(
                name: "IX_Sales_TenantId_UserId",
                table: "Sales",
                columns: new[] { "TenantId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_TenantId_MedicineId",
                table: "SaleItems",
                columns: new[] { "TenantId", "MedicineId" });

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_TenantId_SaleId",
                table: "SaleItems",
                columns: new[] { "TenantId", "SaleId" });

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_TenantId_CategoryId",
                table: "Medicines",
                columns: new[] { "TenantId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Categories_TenantId_CategoryId",
                table: "Medicines",
                columns: new[] { "TenantId", "CategoryId" },
                principalTable: "Categories",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Medicines_TenantId_MedicineId",
                table: "SaleItems",
                columns: new[] { "TenantId", "MedicineId" },
                principalTable: "Medicines",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Sales_TenantId_SaleId",
                table: "SaleItems",
                columns: new[] { "TenantId", "SaleId" },
                principalTable: "Sales",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_TenantId_UserId",
                table: "Sales",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "AspNetUsers",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_TenantId_UserId",
                table: "TenantUsers",
                columns: new[] { "TenantId", "UserId" },
                principalTable: "AspNetUsers",
                principalColumns: new[] { "TenantId", "Id" },
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Medicines_Categories_TenantId_CategoryId",
                table: "Medicines");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Medicines_TenantId_MedicineId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_Sales_TenantId_SaleId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Sales_AspNetUsers_TenantId_UserId",
                table: "Sales");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantUsers_AspNetUsers_TenantId_UserId",
                table: "TenantUsers");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Sales_TenantId_Id",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_Sales_TenantId_UserId",
                table: "Sales");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_TenantId_MedicineId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_TenantId_SaleId",
                table: "SaleItems");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Medicines_TenantId_Id",
                table: "Medicines");

            migrationBuilder.DropIndex(
                name: "IX_Medicines_TenantId_CategoryId",
                table: "Medicines");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_Categories_TenantId_Id",
                table: "Categories");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_AspNetUsers_TenantId_Id",
                table: "AspNetUsers");

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_UserId",
                table: "TenantUsers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_UserId",
                table: "Sales",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_MedicineId",
                table: "SaleItems",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_SaleItems_SaleId",
                table: "SaleItems",
                column: "SaleId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_CategoryId",
                table: "Medicines",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Medicines_Categories_CategoryId",
                table: "Medicines",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Medicines_MedicineId",
                table: "SaleItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SaleItems_Sales_SaleId",
                table: "SaleItems",
                column: "SaleId",
                principalTable: "Sales",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Sales_AspNetUsers_UserId",
                table: "Sales",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantUsers_AspNetUsers_UserId",
                table: "TenantUsers",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
