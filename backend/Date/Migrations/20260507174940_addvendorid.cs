using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class addvendorid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_ApplicationUserId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Products",
                newName: "VendorId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_ApplicationUserId",
                table: "Products",
                newName: "IX_Products_VendorId");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "1917a3f3-eabb-4f5d-8bb5-03a9ec9e581d", "AQAAAAIAAYagAAAAECZH7XJbXt+RxE1EESiHZ3YXOPEmmJVnF9DG36AwLZ2TEedakbzbh+PI0dtdalc+8Q==", "741825c4-eef1-4a4e-8b7a-cac42a2a2679" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_VendorId",
                table: "Products",
                column: "VendorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Products_AspNetUsers_VendorId",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "VendorId",
                table: "Products",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Products_VendorId",
                table: "Products",
                newName: "IX_Products_ApplicationUserId");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "45a8c3f3-0576-4acd-8ebe-37b07a65a639", "AQAAAAIAAYagAAAAEDqY3blZNAaqNLKjymjn4tJEPq2zCRgebaK005FPlNMNtfui4QqId3n7leq5J6uqJg==", "9e9380ec-3d67-416c-ac4c-3c1833aada2a" });

            migrationBuilder.AddForeignKey(
                name: "FK_Products_AspNetUsers_ApplicationUserId",
                table: "Products",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
