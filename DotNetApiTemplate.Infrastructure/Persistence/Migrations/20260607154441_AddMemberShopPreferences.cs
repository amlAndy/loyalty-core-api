using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMemberShopPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemberShopPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReceiveOfferNotifications = table.Column<bool>(type: "bit", nullable: false),
                    ReceiveUpdateNotifications = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SavedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RemovedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberShopPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MemberShopPreferences_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MemberShopPreferences_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemberShopPreferences_BusinessProfileId",
                table: "MemberShopPreferences",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_MemberShopPreferences_MemberProfileId_BusinessProfileId",
                table: "MemberShopPreferences",
                columns: new[] { "MemberProfileId", "BusinessProfileId" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberShopPreferences_MemberProfileId_SavedUtc",
                table: "MemberShopPreferences",
                columns: new[] { "MemberProfileId", "SavedUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemberShopPreferences");
        }
    }
}
