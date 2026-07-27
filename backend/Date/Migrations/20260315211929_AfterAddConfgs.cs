using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class AfterAddConfgs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedSize",
                table: "OrderItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "SelectedSize",
                table: "CartItems",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "storeDescriptions",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "storeName",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedSize",
                table: "OrderItems");

            migrationBuilder.DropColumn(
                name: "SelectedSize",
                table: "CartItems");

            migrationBuilder.DropColumn(
                name: "storeDescriptions",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "storeName",
                table: "AspNetUsers");
        }
    }
}
