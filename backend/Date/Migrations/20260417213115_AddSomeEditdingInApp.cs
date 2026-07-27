using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class AddSomeEditdingInApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShippingAddress",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "Orders",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "45a8c3f3-0576-4acd-8ebe-37b07a65a639", "AQAAAAIAAYagAAAAEDqY3blZNAaqNLKjymjn4tJEPq2zCRgebaK005FPlNMNtfui4QqId3n7leq5J6uqJg==", "9e9380ec-3d67-416c-ac4c-3c1833aada2a" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "ShippingAddress",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "Orders");

            migrationBuilder.UpdateData(
                table: "AspNetUsers",
                keyColumn: "Id",
                keyValue: "b74ddd14-6340-4840-95c2-db12554843e5",
                columns: new[] { "ConcurrencyStamp", "PasswordHash", "SecurityStamp" },
                values: new object[] { "53762dbe-576c-4970-8ed5-5727d161433e", "AQAAAAIAAYagAAAAEG90FyQ4RDxrNr8JnBKf9RZhMYcg9Xlpc5IfUrZbIY7IV6npjyjjTzN85IMDM8+Y/A==", "96ebce78-3c07-4215-b659-60c0493441fb" });
        }
    }
}
