using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ErpBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddManagerSG : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Groups",
                columns: new[] { "Id", "BranchId", "Name" },
                values: new object[] { 3, 2, "Admin Sài Gòn" });

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "Id", "BranchId", "Password", "Role", "Username" },
                values: new object[] { 4, 2, "123", "Manager", "manager_sg" });

            migrationBuilder.InsertData(
                table: "GroupPermissions",
                columns: new[] { "GroupId", "PermissionId" },
                values: new object[,]
                {
                    { 3, 1 },
                    { 3, 2 },
                    { 3, 3 },
                    { 3, 4 },
                    { 3, 5 },
                    { 3, 6 },
                    { 3, 7 }
                });

            migrationBuilder.InsertData(
                table: "UserGroups",
                columns: new[] { "GroupId", "UserId" },
                values: new object[] { 3, 4 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 1 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 2 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 3 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 4 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 5 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 6 });

            migrationBuilder.DeleteData(
                table: "GroupPermissions",
                keyColumns: new[] { "GroupId", "PermissionId" },
                keyValues: new object[] { 3, 7 });

            migrationBuilder.DeleteData(
                table: "UserGroups",
                keyColumns: new[] { "GroupId", "UserId" },
                keyValues: new object[] { 3, 4 });

            migrationBuilder.DeleteData(
                table: "Groups",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: 4);
        }
    }
}
