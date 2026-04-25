using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class MultiBranchAndCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Stock",
                table: "Products");

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Invoices",

                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "ExchangeRate",
                table: "Invoices",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalAmountVND",
                table: "Invoices",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Branches",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(nullable: false),
                    Address = table.Column<string>(nullable: false),
                    Phone = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Branches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CurrencyCode = table.Column<string>(nullable: false),
                    Rate = table.Column<decimal>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BranchStocks",
                columns: table => new
                {
                    ProductId = table.Column<int>(nullable: false),
                    BranchId = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BranchStocks", x => new { x.ProductId, x.BranchId });
                    table.ForeignKey(
                        name: "FK_BranchStocks_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BranchStocks_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StockTransfers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProductId = table.Column<int>(nullable: false),
                    FromBranchId = table.Column<int>(nullable: false),
                    ToBranchId = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    CreatedAt = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_FromBranchId",
                        column: x => x.FromBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Branches_ToBranchId",
                        column: x => x.ToBranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockTransfers_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Branches",
                columns: new[] { "Id", "Address", "Name", "Phone" },
                values: new object[,]
                {
                    { 1, "123 Cầu Giấy, Hà Nội", "Chi nhánh Hà Nội", "024.111.222" },
                    { 2, "456 Quận 1, TP.HCM", "Chi nhánh Sài Gòn", "028.333.444" }
                });

            migrationBuilder.InsertData(
                table: "ExchangeRates",
                columns: new[] { "Id", "CurrencyCode", "Date", "Rate" },
                values: new object[,]
                {
                    { 1, "USD", new DateTime(2026, 4, 23, 0, 0, 0, 0, DateTimeKind.Local), 25450.0m },
                    { 2, "EUR", new DateTime(2026, 4, 23, 0, 0, 0, 0, DateTimeKind.Local), 27120.0m }
                });

            migrationBuilder.CreateIndex(
                name: "IX_BranchStocks_BranchId",
                table: "BranchStocks",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_FromBranchId",
                table: "StockTransfers",
                column: "FromBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ProductId",
                table: "StockTransfers",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ToBranchId",
                table: "StockTransfers",
                column: "ToBranchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BranchStocks");

            migrationBuilder.DropTable(
                name: "ExchangeRates");

            migrationBuilder.DropTable(
                name: "StockTransfers");

            migrationBuilder.DropTable(
                name: "Branches");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ExchangeRate",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TotalAmountVND",
                table: "Invoices");

            migrationBuilder.AddColumn<int>(
                name: "Stock",
                table: "Products",

                nullable: false,
                defaultValue: 0);
        }
    }
}
