using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchToStockLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BranchId",
                table: "StockTransactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_StockTransactions_BranchId",
                table: "StockTransactions",
                column: "BranchId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockTransactions_Branches_BranchId",
                table: "StockTransactions",
                column: "BranchId",
                principalTable: "Branches",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockTransactions_Branches_BranchId",
                table: "StockTransactions");

            migrationBuilder.DropIndex(
                name: "IX_StockTransactions_BranchId",
                table: "StockTransactions");

            migrationBuilder.DropColumn(
                name: "BranchId",
                table: "StockTransactions");
        }
    }
}
