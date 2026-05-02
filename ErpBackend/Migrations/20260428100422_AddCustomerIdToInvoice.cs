using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomerIdToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CustomerId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Date",
                value: new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Date",
                value: new DateTime(2026, 4, 28, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.CreateIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_Customers_CustomerId",
                table: "Invoices");

            migrationBuilder.DropIndex(
                name: "IX_Invoices_CustomerId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "CustomerId",
                table: "Invoices");

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Date",
                value: new DateTime(2026, 4, 27, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Date",
                value: new DateTime(2026, 4, 27, 0, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
