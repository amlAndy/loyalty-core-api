using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DotNetApiTemplate.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLoveBrackleyLoyaltyTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EntityType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EntityId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    BeforeJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BrandReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Town = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ContactEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    SupportEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    PrimaryContactName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TradingHoursSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ParticipationStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Featured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessProfiles_AspNetUsers_OwnerUserId",
                        column: x => x.OwnerUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyPointsRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EarnPointsPerPound = table.Column<int>(type: "int", nullable: false),
                    PencePerPoint = table.Column<int>(type: "int", nullable: false),
                    MaxRedemptionPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    EffectiveFromUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EffectiveToUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyPointsRules", x => x.Id);
                    table.CheckConstraint("CK_LoyaltyPointsRules_Positive", "[EarnPointsPerPound] > 0 AND [PencePerPoint] > 0 AND [MaxRedemptionPercentage] > 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyPointsRules_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MemberProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MobileNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HomeArea = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PublicMemberReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CardCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PointsBalance = table.Column<int>(type: "int", nullable: false),
                    MarketingConsent = table.Column<bool>(type: "bit", nullable: false),
                    EmailUpdatesPreference = table.Column<bool>(type: "bit", nullable: false),
                    LocalOffersPreference = table.Column<bool>(type: "bit", nullable: false),
                    CardReminderPreference = table.Column<bool>(type: "bit", nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemberProfiles", x => x.Id);
                    table.CheckConstraint("CK_MemberProfiles_PointsBalance_NonNegative", "[PointsBalance] >= 0");
                    table.ForeignKey(
                        name: "FK_MemberProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BusinessAlerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Tone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessAlerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessAlerts_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessLocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LocationName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine1 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AddressLine2 = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Town = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Postcode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TillReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TradingHoursSummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessLocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessLocations_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessPayoutPeriods",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PeriodStartUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PeriodEndUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RedeemedAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Fees = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    PayoutTotal = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    PaidUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HeldReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessPayoutPeriods", x => x.Id);
                    table.CheckConstraint("CK_BusinessPayoutPeriods_Money_NonNegative", "[RedeemedAmount] >= 0 AND [Fees] >= 0 AND [PayoutTotal] >= 0");
                    table.ForeignKey(
                        name: "FK_BusinessPayoutPeriods_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BusinessStaffAccesses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    BusinessAccessRole = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PinHash = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    PinStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    LastActivitySummary = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    LastActivityUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BusinessStaffAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BusinessStaffAccesses_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BusinessStaffAccesses_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyCards",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CardCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PublicCardReference = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IssuedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    LinkedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    DisabledUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyCards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoyaltyCards_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoyaltyTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessLocationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    StaffAccessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    PurchaseAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RedeemedValue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CompletedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    HeldReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoyaltyTransactions", x => x.Id);
                    table.CheckConstraint("CK_LoyaltyTransactions_Money_NonNegative", "[PurchaseAmount] >= 0 AND [RedeemedValue] >= 0");
                    table.CheckConstraint("CK_LoyaltyTransactions_Points_Positive", "[Points] > 0");
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_AspNetUsers_ReviewedByUserId",
                        column: x => x.ReviewedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_BusinessLocations_BusinessLocationId",
                        column: x => x.BusinessLocationId,
                        principalTable: "BusinessLocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_BusinessStaffAccesses_StaffAccessId",
                        column: x => x.StaffAccessId,
                        principalTable: "BusinessStaffAccesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoyaltyTransactions_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SecurityAuditEntries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StaffAccessId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Detail = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ReviewedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    EscalatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecurityAuditEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEntries_AspNetUsers_ActorUserId",
                        column: x => x.ActorUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEntries_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SecurityAuditEntries_BusinessStaffAccesses_StaffAccessId",
                        column: x => x.StaffAccessId,
                        principalTable: "BusinessStaffAccesses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AdminReviewCases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CaseType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MemberProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessProfileId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LoyaltyTransactionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    BusinessPayoutPeriodId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ResolutionNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    AssignedToUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ResolvedUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminReviewCases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminReviewCases_AspNetUsers_AssignedToUserId",
                        column: x => x.AssignedToUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminReviewCases_BusinessPayoutPeriods_BusinessPayoutPeriodId",
                        column: x => x.BusinessPayoutPeriodId,
                        principalTable: "BusinessPayoutPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminReviewCases_BusinessProfiles_BusinessProfileId",
                        column: x => x.BusinessProfileId,
                        principalTable: "BusinessProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminReviewCases_LoyaltyTransactions_LoyaltyTransactionId",
                        column: x => x.LoyaltyTransactionId,
                        principalTable: "LoyaltyTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminReviewCases_MemberProfiles_MemberProfileId",
                        column: x => x.MemberProfileId,
                        principalTable: "MemberProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_ActorUserId_CreatedUtc",
                table: "AdminAuditLogs",
                columns: new[] { "ActorUserId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_EntityType_EntityId_CreatedUtc",
                table: "AdminAuditLogs",
                columns: new[] { "EntityType", "EntityId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_AssignedToUserId",
                table: "AdminReviewCases",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_BusinessPayoutPeriodId",
                table: "AdminReviewCases",
                column: "BusinessPayoutPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_BusinessProfileId",
                table: "AdminReviewCases",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_CaseType_Status_Priority",
                table: "AdminReviewCases",
                columns: new[] { "CaseType", "Status", "Priority" });

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_CreatedUtc",
                table: "AdminReviewCases",
                column: "CreatedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_LoyaltyTransactionId",
                table: "AdminReviewCases",
                column: "LoyaltyTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminReviewCases_MemberProfileId",
                table: "AdminReviewCases",
                column: "MemberProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessAlerts_BusinessProfileId_Status_CreatedUtc",
                table: "BusinessAlerts",
                columns: new[] { "BusinessProfileId", "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessLocations_BusinessProfileId",
                table: "BusinessLocations",
                column: "BusinessProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPayoutPeriods_BusinessProfileId_PeriodStartUtc",
                table: "BusinessPayoutPeriods",
                columns: new[] { "BusinessProfileId", "PeriodStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessPayoutPeriods_Status_CreatedUtc",
                table: "BusinessPayoutPeriods",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_BrandReference",
                table: "BusinessProfiles",
                column: "BrandReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_OwnerUserId",
                table: "BusinessProfiles",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_BusinessProfiles_ParticipationStatus_Featured",
                table: "BusinessProfiles",
                columns: new[] { "ParticipationStatus", "Featured" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffAccesses_BusinessProfileId_PinStatus",
                table: "BusinessStaffAccesses",
                columns: new[] { "BusinessProfileId", "PinStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_BusinessStaffAccesses_UserId",
                table: "BusinessStaffAccesses",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCards_CardCode",
                table: "LoyaltyCards",
                column: "CardCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCards_MemberProfileId_IsPrimary",
                table: "LoyaltyCards",
                columns: new[] { "MemberProfileId", "IsPrimary" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyCards_PublicCardReference",
                table: "LoyaltyCards",
                column: "PublicCardReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsRules_CreatedByUserId",
                table: "LoyaltyPointsRules",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyPointsRules_IsActive_EffectiveFromUtc",
                table: "LoyaltyPointsRules",
                columns: new[] { "IsActive", "EffectiveFromUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_BusinessLocationId",
                table: "LoyaltyTransactions",
                column: "BusinessLocationId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_BusinessProfileId_CreatedUtc",
                table: "LoyaltyTransactions",
                columns: new[] { "BusinessProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_MemberProfileId_CreatedUtc",
                table: "LoyaltyTransactions",
                columns: new[] { "MemberProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_Reference",
                table: "LoyaltyTransactions",
                column: "Reference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_ReviewedByUserId",
                table: "LoyaltyTransactions",
                column: "ReviewedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_StaffAccessId",
                table: "LoyaltyTransactions",
                column: "StaffAccessId");

            migrationBuilder.CreateIndex(
                name: "IX_LoyaltyTransactions_Status_CreatedUtc",
                table: "LoyaltyTransactions",
                columns: new[] { "Status", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_CardCode",
                table: "MemberProfiles",
                column: "CardCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_PublicMemberReference",
                table: "MemberProfiles",
                column: "PublicMemberReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemberProfiles_UserId",
                table: "MemberProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEntries_ActorUserId",
                table: "SecurityAuditEntries",
                column: "ActorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEntries_BusinessProfileId_CreatedUtc",
                table: "SecurityAuditEntries",
                columns: new[] { "BusinessProfileId", "CreatedUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SecurityAuditEntries_StaffAccessId",
                table: "SecurityAuditEntries",
                column: "StaffAccessId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AdminReviewCases");

            migrationBuilder.DropTable(
                name: "BusinessAlerts");

            migrationBuilder.DropTable(
                name: "LoyaltyCards");

            migrationBuilder.DropTable(
                name: "LoyaltyPointsRules");

            migrationBuilder.DropTable(
                name: "SecurityAuditEntries");

            migrationBuilder.DropTable(
                name: "BusinessPayoutPeriods");

            migrationBuilder.DropTable(
                name: "LoyaltyTransactions");

            migrationBuilder.DropTable(
                name: "BusinessLocations");

            migrationBuilder.DropTable(
                name: "BusinessStaffAccesses");

            migrationBuilder.DropTable(
                name: "MemberProfiles");

            migrationBuilder.DropTable(
                name: "BusinessProfiles");
        }
    }
}
