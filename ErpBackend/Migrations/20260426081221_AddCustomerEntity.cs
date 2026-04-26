using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false),
                    Phone = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    TaxId = table.Column<string>(nullable: true),
                    CreatedAt = table.Column<DateTime>(nullable: false),
                    BranchId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Customers_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id");
                });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Name" },
                values: new object[] { "123 C?u Gi?y, Hà N?i", "Chi nhánh Hà N?i" });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 2,
                column: "Address",
                value: "456 Qu?n 1, TP.HCM");

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Date",
                value: new DateTime(2026, 4, 26, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Date",
                value: new DateTime(2026, 4, 26, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "K? toán kho Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Sales Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Admin Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "K? toán kho Sài Gòn");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Xem s?n ph?m");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Thêm s?n ph?m");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Duy?t hóa don");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "Xu?t báo cáo");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Nh?p kho");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "T?o hóa don");

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_BranchId",
                table: "Invoices",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_Customers_BranchId",
                table: "Customers",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Branches_BranchId",
                table: "Invoices",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Branches_BranchId",
                table: "Invoices");

            migrationBuilder.DropTable(
                name: "Customers");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_BranchId",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Address", "Name" },
                values: new object[] { "123 C?u Gi?y, Hà N?i", "Chi nhánh Hà N?i" });

            migrationBuilder.UpdateData(
                table: "Branches",
                keyColumn: "Id",
                keyValue: 2,
                column: "Address",
                value: "456 Qu?n 1, TP.HCM");

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Date",
                value: new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Date",
                value: new DateTime(2026, 4, 25, 0, 0, 0, 0, DateTimeKind.Local));

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "K? toán kho Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Sales Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Admin Hà N?i");

            migrationBuilder.UpdateData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "K? toán kho Sài Gòn");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 1,
                column: "Description",
                value: "Xem s?n ph?m");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 2,
                column: "Description",
                value: "Thêm s?n ph?m");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 3,
                column: "Description",
                value: "Duy?t hóa don");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 4,
                column: "Description",
                value: "Xu?t báo cáo");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 5,
                column: "Description",
                value: "Nh?p kho");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "Id",
                keyValue: 6,
                column: "Description",
                value: "T?o hóa don");
        }
    }
}
