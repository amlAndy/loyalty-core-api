using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTownAndBusinessCategoryLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var createdUtc = new DateTimeOffset(2026, 3, 1, 9, 0, 0, TimeSpan.Zero);
            var brackleyTownId = new Guid("10000000-0000-0000-0000-000000000001");
            var foodAndDrinkCategoryId = new Guid("20000000-0000-0000-0000-000000000001");
            var retailCategoryId = new Guid("20000000-0000-0000-0000-000000000002");
            var floristCategoryId = new Guid("20000000-0000-0000-0000-000000000003");
            var booksCategoryId = new Guid("20000000-0000-0000-0000-000000000004");
            var homeAndGiftCategoryId = new Guid("20000000-0000-0000-0000-000000000005");
            var coffeeShopCategoryId = new Guid("20000000-0000-0000-0000-000000000006");
            var groceriesCategoryId = new Guid("20000000-0000-0000-0000-000000000007");

            migrationBuilder.CreateTable(
                name: "BusinessCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Towns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Slug = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PrimaryDomain = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Towns", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Towns",
                columns: new[] { "Id", "Name", "Slug", "DisplayName", "PrimaryDomain", "IsActive", "DisplayOrder", "CreatedUtc", "UpdatedUtc" },
                values: new object[] { brackleyTownId, "Brackley", "brackley", "Brackley", null, true, 1, createdUtc, null });

            migrationBuilder.InsertData(
                table: "BusinessCategories",
                columns: new[] { "Id", "Name", "Slug", "Description", "IsActive", "DisplayOrder", "CreatedUtc", "UpdatedUtc" },
                values: new object[,]
                {
                    { foodAndDrinkCategoryId, "Food and Drink", "food-and-drink", "Restaurants, cafés, delis, and hospitality.", true, 1, createdUtc, null },
                    { retailCategoryId, "Retail", "retail", "Independent shops and local retail services.", true, 2, createdUtc, null },
                    { floristCategoryId, "Florist", "florist", "Florists and flower arrangement specialists.", true, 3, createdUtc, null },
                    { booksCategoryId, "Books", "books", "Bookshops, cards, and reading events.", true, 4, createdUtc, null },
                    { homeAndGiftCategoryId, "Home and Gift", "home-and-gift", "Homeware, gifts, and local presents.", true, 5, createdUtc, null },
                    { coffeeShopCategoryId, "Coffee Shop", "coffee-shop", "Coffee, cakes, brunch, and light refreshments.", true, 6, createdUtc, null },
                    { groceriesCategoryId, "Groceries", "groceries", "Grocers, pantries, delis, and local provisions.", true, 7, createdUtc, null }
                });

            migrationBuilder.AddColumn<Guid>(
                name: "BusinessCategoryId",
                table: "BusinessProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: foodAndDrinkCategoryId);

            migrationBuilder.AddColumn<Guid>(
                name: "TownId",
                table: "BusinessProfiles",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: brackleyTownId);

            migrationBuilder.Sql($"""
                UPDATE [BusinessProfiles]
                SET [TownId] = '{brackleyTownId}';

                UPDATE [BusinessProfiles]
                SET [BusinessCategoryId] = CASE
                    WHEN [Category] LIKE '%Florist%' THEN '{floristCategoryId}'
                    WHEN [Category] LIKE '%Book%' THEN '{booksCategoryId}'
                    WHEN [Category] LIKE '%Coffee%' THEN '{coffeeShopCategoryId}'
                    WHEN [Category] LIKE '%Home%' OR [Category] LIKE '%Gift%' THEN '{homeAndGiftCategoryId}'
                    WHEN [Category] LIKE '%Deli%' OR [Category] LIKE '%Grocery%' OR [Name] LIKE '%Pantry%' THEN '{groceriesCategoryId}'
                    WHEN [Category] LIKE '%Retail%' THEN '{retailCategoryId}'
                    ELSE '{foodAndDrinkCategoryId}'
                END;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_BusinessCategoryId",
                table: "BusinessProfiles",
                column: "BusinessCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_TownId",
                table: "BusinessProfiles",
                column: "TownId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCategories_IsActive_DisplayOrder",
                table: "BusinessCategories",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessCategories_Slug",
                table: "BusinessCategories",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Towns_IsActive_DisplayOrder",
                table: "Towns",
                columns: new[] { "IsActive", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_Towns_Slug",
                table: "Towns",
                column: "Slug",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessProfiles_BusinessCategories_BusinessCategoryId",
                table: "BusinessProfiles",
                column: "BusinessCategoryId",
                principalTable: "BusinessCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BusinessProfiles_Towns_TownId",
                table: "BusinessProfiles",
                column: "TownId",
                principalTable: "Towns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BusinessProfiles_BusinessCategories_BusinessCategoryId",
                table: "BusinessProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_BusinessProfiles_Towns_TownId",
                table: "BusinessProfiles");

            migrationBuilder.DropIndex(
                name: "IX_BusinessProfiles_BusinessCategoryId",
                table: "BusinessProfiles");

            migrationBuilder.DropIndex(
                name: "IX_BusinessProfiles_TownId",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "BusinessCategoryId",
                table: "BusinessProfiles");

            migrationBuilder.DropColumn(
                name: "TownId",
                table: "BusinessProfiles");

            migrationBuilder.DropTable(
                name: "BusinessCategories");

            migrationBuilder.DropTable(
                name: "Towns");
        }
    }
}
