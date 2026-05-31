using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ActingUserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActingUser = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ActionLabel = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EntityType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    TargetId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Target = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditEntryDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuditEntryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Sequence = table.Column<int>(type: "int", nullable: false),
                    Field = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BeforeValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterValueJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEntryDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEntryDetails_AuditEntries_AuditEntryId",
                        column: x => x.AuditEntryId,
                        principalTable: "AuditEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_ActingUser",
                table: "AuditEntries",
                column: "ActingUser");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_Action_OccurredAtUtc",
                table: "AuditEntries",
                columns: new[] { "Action", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_EntityType_OccurredAtUtc",
                table: "AuditEntries",
                columns: new[] { "EntityType", "OccurredAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_OccurredAtUtc",
                table: "AuditEntries",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntries_TargetId",
                table: "AuditEntries",
                column: "TargetId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEntryDetails_AuditEntryId_Sequence",
                table: "AuditEntryDetails",
                columns: new[] { "AuditEntryId", "Sequence" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditEntryDetails");

            migrationBuilder.DropTable(
                name: "AuditEntries");
        }
    }
}
