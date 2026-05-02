using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddAccountingModuleFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ExpenseAccountId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IncomeAccountId",
                table: "Products",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AccountTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    NormalBalance = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    EntryDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Reference = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    BranchId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntries_Branches_BranchId",
                        column: x => x.BranchId,
                        principalTable: "Branches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Code = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    AccountTypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    ParentId = table.Column<int>(type: "INTEGER", nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountTypes_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "AccountTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accounts_Accounts_ParentId",
                        column: x => x.ParentId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JournalEntryLines",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    JournalEntryId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AccountId = table.Column<int>(type: "INTEGER", nullable: false),
                    Debit = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    Credit = table.Column<decimal>(type: "decimal(18,4)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JournalEntryLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JournalEntryLines_JournalEntries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "JournalEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AccountTypes",
                columns: new[] { "Id", "Name", "NormalBalance" },
                values: new object[,]
                {
                    { 1, "Tài sản (Asset)", "Debit" },
                    { 2, "Nợ phải trả (Liability)", "Credit" },
                    { 3, "Vốn chủ sở hữu (Equity)", "Credit" },
                    { 4, "Doanh thu (Revenue)", "Credit" },
                    { 5, "Chi phí (Expense)", "Debit" }
                });

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 1,
                column: "Date",
                value: new DateTime(2026, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "ExchangeRates",
                keyColumn: "Id",
                keyValue: 2,
                column: "Date",
                value: new DateTime(2026, 5, 2, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.InsertData(
                table: "Accounts",
                columns: new[] { "Id", "AccountTypeId", "Code", "IsActive", "Name", "ParentId" },
                values: new object[,]
                {
                    { 1, 1, "111", true, "Tiền mặt", null },
                    { 2, 1, "112", true, "Tiền gửi ngân hàng", null },
                    { 3, 1, "131", true, "Phải thu khách hàng", null },
                    { 4, 1, "156", true, "Hàng hóa", null },
                    { 5, 2, "331", true, "Phải trả người bán", null },
                    { 6, 4, "511", true, "Doanh thu bán hàng", null },
                    { 7, 5, "632", true, "Giá vốn hàng bán", null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ExpenseAccountId",
                table: "Products",
                column: "ExpenseAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IncomeAccountId",
                table: "Products",
                column: "IncomeAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_Code",
                table: "Accounts",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_ParentId",
                table: "Accounts",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntries_BranchId",
                table: "JournalEntries",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_AccountId",
                table: "JournalEntryLines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_JournalEntryLines_JournalEntryId",
                table: "JournalEntryLines",
                column: "JournalEntryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Accounts_ExpenseAccountId",
                table: "Products",
                column: "ExpenseAccountId",
                principalTable: "Accounts",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Products_Accounts_IncomeAccountId",
                table: "Products",
                column: "IncomeAccountId",
                principalTable: "Accounts",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_Accounts_ExpenseAccountId",
                table: "Products");

            migrationBuilder.DropForeignKey(
                name: "FK_Products_Accounts_IncomeAccountId",
                table: "Products");

            migrationBuilder.DropTable(
                name: "JournalEntryLines");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "JournalEntries");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropIndex(
                name: "IX_Products_ExpenseAccountId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IncomeAccountId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ExpenseAccountId",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IncomeAccountId",
                table: "Products");

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
        }
    }
}
