using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSeededUserAndFoundationCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Address", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberAddingKeyofCountry", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "storeDescriptions", "storeName" },
                values: new object[] { "b74ddd14-6340-4840-95c2-db12554843e5", 0, null, "1917a3f3-eabb-4f5d-8bb5-03a9ec9e581d", "test@user.com", true, "Ahmed", "Ali", false, null, "TEST@USER.COM", "TEST@USER.COM", "AQAAAAIAAYagAAAAECZH7XJbXt+RxE1EESiHZ3YXOPEmmJVnF9DG36AwLZ2TEedakbzbh+PI0dtdalc+8Q==", null, 0, false, "741825c4-eef1-4a4e-8b7a-cac42a2a2679", false, "test@user.com", null, null });
        }
    }
}
