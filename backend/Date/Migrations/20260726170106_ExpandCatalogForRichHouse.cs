using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Marketify.Migrations
{
    /// <inheritdoc />
    public partial class ExpandCatalogForRichHouse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sizes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Sizes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Products",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompareAtPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "CreatedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<string>(
                name: "ImportKey",
                table: "Products",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBestSeller",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeatured",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsNewArrival",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OfferEnd",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "OfferPrice",
                table: "Products",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OfferStart",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "Products",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "Products",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortDescription",
                table: "Products",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Sku",
                table: "Products",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Products",
                type: "nvarchar(220)",
                maxLength: 220,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "Products",
                type: "nvarchar(24)",
                maxLength: 24,
                nullable: false,
                defaultValue: "Draft");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UpdatedAt",
                table: "Products",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ProductImages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "AltText",
                table: "ProductImages",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Height",
                table: "ProductImages",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OriginalFileName",
                table: "ProductImages",
                type: "nvarchar(260)",
                maxLength: 260,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "ProductImages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SourceHash",
                table: "ProductImages",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Width",
                table: "ProductImages",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(120)",
                maxLength: 120,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DisplayOrder",
                table: "Categories",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Categories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Categories",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoDescription",
                table: "Categories",
                type: "nvarchar(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SeoTitle",
                table: "Categories",
                type: "nvarchar(160)",
                maxLength: 160,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Slug",
                table: "Categories",
                type: "nvarchar(140)",
                maxLength: 140,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ProductVariants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    OptionKey = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    SizeId = table.Column<int>(type: "int", nullable: true),
                    ColorName = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    ColorSlug = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PriceOverride = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    StockQuantity = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductVariants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProductVariants_Sizes_SizeId",
                        column: x => x.SizeId,
                        principalTable: "Sizes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 8,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 9,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 10,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 11,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 12,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 13,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 14,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 15,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 16,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 17,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 18,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 19,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 20,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 21,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 22,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 23,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 24,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 25,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 26,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 27,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 28,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 29,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: 30,
                columns: new[] { "Description", "DisplayOrder", "ImageUrl", "IsActive", "SeoDescription", "SeoTitle", "Slug" },
                values: new object[] { null, 0, null, true, null, null, null });

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "S", 10 });

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "M", 20 });

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "L", 30 });

            migrationBuilder.InsertData(
                table: "Sizes",
                columns: new[] { "Id", "Name", "SortOrder" },
                values: new object[,]
                {
                    { 4, "XL", 40 },
                    { 5, "XXL", 50 },
                    { 20, "46", 460 },
                    { 21, "48", 480 },
                    { 22, "50", 500 },
                    { 23, "52", 520 },
                    { 24, "54", 540 },
                    { 25, "56", 560 },
                    { 26, "58", 580 },
                    { 27, "60", 600 },
                    { 40, "40", 400 },
                    { 41, "41", 410 },
                    { 42, "42", 420 },
                    { 43, "43", 430 },
                    { 44, "44", 440 },
                    { 45, "45", 450 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Products_ImportKey",
                table: "Products",
                column: "ImportKey",
                unique: true,
                filter: "[ImportKey] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsActive_CategoryId",
                table: "Products",
                columns: new[] { "IsActive", "CategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsBestSeller_IsActive",
                table: "Products",
                columns: new[] { "IsBestSeller", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsFeatured_IsActive",
                table: "Products",
                columns: new[] { "IsFeatured", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_IsNewArrival_IsActive",
                table: "Products",
                columns: new[] { "IsNewArrival", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Products_Sku",
                table: "Products",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Products_Slug",
                table: "Products",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SortOrder",
                table: "ProductImages",
                columns: new[] { "ProductId", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId_SourceHash",
                table: "ProductImages",
                columns: new[] { "ProductId", "SourceHash" },
                unique: true,
                filter: "[SourceHash] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Categories_IsActive_DisplayOrder",
                table: "Categories",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Categories_Slug",
                table: "Categories",
                column: "Slug",
                unique: true,
                filter: "[Slug] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_IsActive",
                table: "ProductVariants",
                columns: new[] { "ProductId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_ProductId_OptionKey",
                table: "ProductVariants",
                columns: new[] { "ProductId", "OptionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_SizeId",
                table: "ProductVariants",
                column: "SizeId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductVariants_Sku",
                table: "ProductVariants",
                column: "Sku",
                unique: true,
                filter: "[Sku] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductVariants");

            migrationBuilder.DropIndex(
                name: "IX_Products_ImportKey",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsActive_CategoryId",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsBestSeller_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsFeatured_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_IsNewArrival_IsActive",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Sku",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Products_Slug",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_SortOrder",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_ProductImages_ProductId_SourceHash",
                table: "ProductImages");

            migrationBuilder.DropIndex(
                name: "IX_Categories_IsActive_DisplayOrder",
                table: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Categories_Slug",
                table: "Categories");

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 45);

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Sizes");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CompareAtPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ImportKey",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsBestSeller",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsFeatured",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsNewArrival",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OfferEnd",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OfferPrice",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "OfferStart",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ShortDescription",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Sku",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "AltText",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "OriginalFileName",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "SourceHash",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Width",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "DisplayOrder",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoDescription",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "SeoTitle",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "Slug",
                table: "Categories");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Sizes",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(30)",
                oldMaxLength: 30);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "Products",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "ProductImages",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "Categories",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(120)",
                oldMaxLength: 120);

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Small");

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 2,
                column: "Name",
                value: "Medium");

            migrationBuilder.UpdateData(
                table: "Sizes",
                keyColumn: "Id",
                keyValue: 3,
                column: "Name",
                value: "Large");

            migrationBuilder.CreateIndex(
                name: "IX_ProductImages_ProductId",
                table: "ProductImages",
                column: "ProductId");
        }
    }
}
