using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBusinessStaffAccessStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessStatus",
                table: "BusinessStaffAccesses",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "Suspended");

            migrationBuilder.Sql("""
                UPDATE [BusinessStaffAccesses]
                SET [AccessStatus] = N'Suspended';

                UPDATE [BusinessStaffAccesses]
                SET [AccessStatus] = N'Active'
                WHERE UPPER(ISNULL([PinStatus], N'')) = N'ACTIVE'
                  AND [UserId] IS NOT NULL
                  AND LTRIM(RTRIM([UserId])) <> N'';
                """);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffAccesses_BusinessProfileId_AccessStatus",
                table: "BusinessStaffAccesses",
                columns: new[] { "BusinessProfileId", "AccessStatus" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_BusinessStaffAccesses_BusinessProfileId_AccessStatus",
                table: "BusinessStaffAccesses");

            migrationBuilder.DropColumn(
                name: "AccessStatus",
                table: "BusinessStaffAccesses");
        }
    }
}
