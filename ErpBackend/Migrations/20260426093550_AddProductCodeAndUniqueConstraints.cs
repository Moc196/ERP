using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddProductCodeAndUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductCode",
                table: "Products",
                nullable: false,
                defaultValue: "");

            // 1. Xử lý trùng tên
            migrationBuilder.Sql(@"
                UPDATE ""Products"" 
                SET ""Name"" = ""Name"" || ' (Trùng - ' || ""Id"" || ')' 
                WHERE ""Id"" IN (
                    SELECT ""Id"" FROM (
                        SELECT ""Id"", ROW_NUMBER() OVER (PARTITION BY ""Name"" ORDER BY ""Id"") as row_num 
                        FROM ""Products""
                    ) t WHERE t.row_num > 1
                )");

            // 2. Gán mã SP cho hàng cũ
            migrationBuilder.Sql(@"
                UPDATE ""Products"" 
                SET ""ProductCode"" = 'SP-OLD-' || ""Id"" 
                WHERE ""ProductCode"" = ''");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Name",
                table: "Products",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Products_ProductCode",
                table: "Products",
                column: "ProductCode",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Products_Name",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_ProductCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ProductCode",
                table: "Products");
        }
    }
}
