using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationRoleMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRootRole",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSystemRole",
                table: "AspNetRoles",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ParentRoleId",
                table: "AspNetRoles",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_ParentRoleId",
                table: "AspNetRoles",
                column: "ParentRoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_AspNetRoles_AspNetRoles_ParentRoleId",
                table: "AspNetRoles",
                column: "ParentRoleId",
                principalTable: "AspNetRoles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AspNetRoles_AspNetRoles_ParentRoleId",
                table: "AspNetRoles");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_ParentRoleId",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsRootRole",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "IsSystemRole",
                table: "AspNetRoles");

            migrationBuilder.DropColumn(
                name: "ParentRoleId",
                table: "AspNetRoles");
        }
    }
}
