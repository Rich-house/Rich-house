using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class SeedUser_Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "AspNetUsers",
                columns: new[] { "Id", "AccessFailedCount", "Address", "ConcurrencyStamp", "Email", "EmailConfirmed", "FirstName", "LastName", "LockoutEnabled", "LockoutEnd", "NormalizedEmail", "NormalizedUserName", "PasswordHash", "PhoneNumber", "PhoneNumberConfirmed", "SecurityStamp", "TwoFactorEnabled", "UserName", "storeDescriptions", "storeName" },
                values: new object[] { "b74ddd14-6340-4840-95c2-db12554843e5", 0, null, "7e3a858a-b58d-49c8-96e4-16d148067f7d", "test@user.com", true, "Ahmed", "Ali", false, null, "TEST@USER.COM", "TEST@USER.COM", "AQAAAAIAAYagAAAAEFnPgIm1S4BilwaH+lcNkAYkOJj6GT8zvtsOC2oEgOTeQGNMBcs0aaLVtgE5gsWjqw==", null, false, "f92a5f1d-3237-46d9-8933-174943754768", false, "test@user.com", null, null });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5");
        }
    }
}
