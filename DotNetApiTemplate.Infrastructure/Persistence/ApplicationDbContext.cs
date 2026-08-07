using DotNetApiTemplate.Infrastructure.Auth;
using DotNetApiTemplate.Infrastructure.Admin.Audit;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.LoveBrackley;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Persistence;

public sealed class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<AuditChangeDetailEntity> AuditChangeDetails => Set<AuditChangeDetailEntity>();
    public DbSet<AuditEntryEntity> AuditEntries => Set<AuditEntryEntity>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AdminAuditLogEntity> AdminAuditLogs => Set<AdminAuditLogEntity>();
    public DbSet<AdminReviewCaseEntity> AdminReviewCases => Set<AdminReviewCaseEntity>();
    public DbSet<BusinessAlertEntity> BusinessAlerts => Set<BusinessAlertEntity>();
    public DbSet<BusinessLocationEntity> BusinessLocations => Set<BusinessLocationEntity>();
    public DbSet<BusinessCategoryEntity> BusinessCategories => Set<BusinessCategoryEntity>();
    public DbSet<BusinessPayoutPeriodEntity> BusinessPayoutPeriods => Set<BusinessPayoutPeriodEntity>();
    public DbSet<BusinessProfileEntity> BusinessProfiles => Set<BusinessProfileEntity>();
    public DbSet<BusinessStaffAccessEntity> BusinessStaffAccesses => Set<BusinessStaffAccessEntity>();
    public DbSet<LoyaltyCardEntity> LoyaltyCards => Set<LoyaltyCardEntity>();
    public DbSet<LoyaltyPointsRuleEntity> LoyaltyPointsRules => Set<LoyaltyPointsRuleEntity>();
    public DbSet<LoyaltyTransactionEntity> LoyaltyTransactions => Set<LoyaltyTransactionEntity>();
    public DbSet<MemberProfileEntity> MemberProfiles => Set<MemberProfileEntity>();
    public DbSet<MemberShopPreferenceEntity> MemberShopPreferences => Set<MemberShopPreferenceEntity>();
    public DbSet<SecurityAuditEntryEntity> SecurityAuditEntries => Set<SecurityAuditEntryEntity>();
    public DbSet<TownEntity> Towns => Set<TownEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(user => user.Status)
                .IsRequired()
                .HasMaxLength(50);
        });

        builder.Entity<ApplicationRole>(entity =>
        {
            entity.Property(role => role.Description)
                .IsRequired()
                .HasDefaultValue(string.Empty);

            entity.Property(role => role.CreatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(role => role.UpdatedAtUtc)
                .HasDefaultValueSql("SYSUTCDATETIME()");

            entity.Property(role => role.IsRootRole)
                .HasDefaultValue(false);

            entity.Property(role => role.IsSystemRole)
                .HasDefaultValue(false);

            entity.HasIndex(role => role.ParentRoleId);

            entity.HasOne(role => role.ParentRole)
                .WithMany(role => role.ChildRoles)
                .HasForeignKey(role => role.ParentRoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AuditEntryEntity>(entity =>
        {
            entity.ToTable("AuditEntries");

            entity.HasKey(entry => entry.Id);

            entity.Property(entry => entry.ActingUser)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(entry => entry.ActionLabel)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.EntityType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(entry => entry.TargetId)
                .IsRequired()
                .HasMaxLength(450);

            entity.Property(entry => entry.TargetName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Target)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(entry => entry.Summary)
                .IsRequired()
                .HasMaxLength(1000);

            entity.HasIndex(entry => entry.OccurredAtUtc);
            entity.HasIndex(entry => new { entry.Action, entry.OccurredAtUtc });
            entity.HasIndex(entry => new { entry.EntityType, entry.OccurredAtUtc });
            entity.HasIndex(entry => entry.ActingUser);
            entity.HasIndex(entry => entry.TargetId);

            entity.HasMany(entry => entry.Details)
                .WithOne(detail => detail.AuditEntry)
                .HasForeignKey(detail => detail.AuditEntryId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<AuditChangeDetailEntity>(entity =>
        {
            entity.ToTable("AuditEntryDetails");

            entity.HasKey(detail => detail.Id);

            entity.Property(detail => detail.Field)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(detail => detail.Label)
                .IsRequired()
                .HasMaxLength(200);

            entity.HasIndex(detail => new { detail.AuditEntryId, detail.Sequence });
        });

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(refreshToken => refreshToken.Id);

            entity.Property(refreshToken => refreshToken.Token)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(refreshToken => refreshToken.ActiveRoleId)
                .HasMaxLength(450);

            entity.Property(refreshToken => refreshToken.CreatedByIp)
                .HasMaxLength(45);

            entity.Property(refreshToken => refreshToken.RevokedByIp)
                .HasMaxLength(45);

            entity.Property(refreshToken => refreshToken.ReplacedByToken)
                .HasMaxLength(200);

            entity.HasIndex(refreshToken => refreshToken.Token)
                .IsUnique();

            entity.HasIndex(refreshToken => refreshToken.UserId);

            entity.HasOne(refreshToken => refreshToken.User)
                .WithMany(user => user.RefreshTokens)
                .HasForeignKey(refreshToken => refreshToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(passwordResetToken => passwordResetToken.Id);

            entity.Property(passwordResetToken => passwordResetToken.TokenHash)
                .IsRequired()
                .HasMaxLength(64);

            entity.HasIndex(passwordResetToken => passwordResetToken.TokenHash)
                .IsUnique();

            entity.HasIndex(passwordResetToken => passwordResetToken.UserId);
            entity.HasIndex(passwordResetToken => passwordResetToken.ExpiresAtUtc);

            entity.HasOne(passwordResetToken => passwordResetToken.User)
                .WithMany(user => user.PasswordResetTokens)
                .HasForeignKey(passwordResetToken => passwordResetToken.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        ConfigureLoveBrackley(builder);
    }

    private static void ConfigureLoveBrackley(ModelBuilder builder)
    {
        builder.Entity<MemberProfileEntity>(entity =>
        {
            entity.ToTable("MemberProfiles");
            entity.HasKey(member => member.Id);
            entity.Property(member => member.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(member => member.Email).HasMaxLength(256);
            entity.Property(member => member.MobileNumber).HasMaxLength(50);
            entity.Property(member => member.HomeArea).HasMaxLength(100);
            entity.Property(member => member.PublicMemberReference).IsRequired().HasMaxLength(50);
            entity.Property(member => member.CardCode).IsRequired().HasMaxLength(50);
            entity.Property(member => member.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(member => member.UserId).IsUnique();
            entity.HasIndex(member => member.PublicMemberReference).IsUnique();
            entity.HasIndex(member => member.CardCode).IsUnique();
            entity.ToTable(table => table.HasCheckConstraint("CK_MemberProfiles_PointsBalance_NonNegative", "[PointsBalance] >= 0"));
            entity.HasOne(member => member.User)
                .WithOne()
                .HasForeignKey<MemberProfileEntity>(member => member.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoyaltyCardEntity>(entity =>
        {
            entity.ToTable("LoyaltyCards");
            entity.HasKey(card => card.Id);
            entity.Property(card => card.CardCode).IsRequired().HasMaxLength(50);
            entity.Property(card => card.PublicCardReference).IsRequired().HasMaxLength(50);
            entity.Property(card => card.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(card => card.CardCode).IsUnique();
            entity.HasIndex(card => card.PublicCardReference).IsUnique();
            entity.HasIndex(card => new { card.MemberProfileId, card.IsPrimary });
            entity.HasOne(card => card.MemberProfile)
                .WithMany(member => member.LoyaltyCards)
                .HasForeignKey(card => card.MemberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<TownEntity>(entity =>
        {
            entity.ToTable("Towns");
            entity.HasKey(town => town.Id);
            entity.Property(town => town.Name).IsRequired().HasMaxLength(150);
            entity.Property(town => town.Slug).IsRequired().HasMaxLength(150);
            entity.Property(town => town.DisplayName).IsRequired().HasMaxLength(150);
            entity.Property(town => town.PrimaryDomain).HasMaxLength(250);
            entity.HasIndex(town => town.Slug).IsUnique();
            entity.HasIndex(town => new { town.IsActive, town.DisplayOrder });
        });

        builder.Entity<BusinessCategoryEntity>(entity =>
        {
            entity.ToTable("BusinessCategories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).IsRequired().HasMaxLength(150);
            entity.Property(category => category.Slug).IsRequired().HasMaxLength(150);
            entity.Property(category => category.Description).HasMaxLength(500);
            entity.HasIndex(category => category.Slug).IsUnique();
            entity.HasIndex(category => new { category.IsActive, category.DisplayOrder });
        });

        builder.Entity<BusinessProfileEntity>(entity =>
        {
            entity.ToTable("BusinessProfiles");
            entity.HasKey(business => business.Id);
            entity.Property(business => business.Name).IsRequired().HasMaxLength(200);
            entity.Property(business => business.BrandReference).IsRequired().HasMaxLength(50);
            entity.Property(business => business.Category).IsRequired().HasMaxLength(100);
            entity.Property(business => business.Description).IsRequired().HasMaxLength(1000);
            entity.Property(business => business.LocationName).IsRequired().HasMaxLength(200);
            entity.Property(business => business.AddressLine1).IsRequired().HasMaxLength(200);
            entity.Property(business => business.AddressLine2).HasMaxLength(200);
            entity.Property(business => business.Town).IsRequired().HasMaxLength(100);
            entity.Property(business => business.Postcode).IsRequired().HasMaxLength(20);
            entity.Property(business => business.ContactEmail).IsRequired().HasMaxLength(256);
            entity.Property(business => business.SupportEmail).HasMaxLength(256);
            entity.Property(business => business.PrimaryContactName).IsRequired().HasMaxLength(200);
            entity.Property(business => business.TradingHoursSummary).IsRequired().HasMaxLength(500);
            entity.Property(business => business.ParticipationStatus).IsRequired().HasMaxLength(50);
            entity.HasIndex(business => business.BrandReference).IsUnique();
            entity.HasIndex(business => business.OwnerUserId);
            entity.HasIndex(business => business.TownId);
            entity.HasIndex(business => business.BusinessCategoryId);
            entity.HasIndex(business => new { business.ParticipationStatus, business.Featured });
            entity.HasOne(business => business.OwnerUser)
                .WithMany()
                .HasForeignKey(business => business.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(business => business.TownLookup)
                .WithMany(town => town.BusinessProfiles)
                .HasForeignKey(business => business.TownId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(business => business.BusinessCategory)
                .WithMany(category => category.BusinessProfiles)
                .HasForeignKey(business => business.BusinessCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessLocationEntity>(entity =>
        {
            entity.ToTable("BusinessLocations");
            entity.HasKey(location => location.Id);
            entity.Property(location => location.LocationName).IsRequired().HasMaxLength(200);
            entity.Property(location => location.AddressLine1).IsRequired().HasMaxLength(200);
            entity.Property(location => location.AddressLine2).HasMaxLength(200);
            entity.Property(location => location.Town).IsRequired().HasMaxLength(100);
            entity.Property(location => location.Postcode).IsRequired().HasMaxLength(20);
            entity.Property(location => location.TillReference).IsRequired().HasMaxLength(100);
            entity.Property(location => location.TradingHoursSummary).IsRequired().HasMaxLength(500);
            entity.Property(location => location.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(location => location.BusinessProfileId);
            entity.HasOne(location => location.BusinessProfile)
                .WithMany(business => business.Locations)
                .HasForeignKey(location => location.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BusinessStaffAccessEntity>(entity =>
        {
            entity.ToTable("BusinessStaffAccesses");
            entity.HasKey(staff => staff.Id);
            entity.Property(staff => staff.UserId).HasMaxLength(450);
            entity.Property(staff => staff.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(staff => staff.BusinessAccessRole).IsRequired().HasMaxLength(50);
            entity.Property(staff => staff.PinHash).IsRequired().HasMaxLength(256);
            entity.Property(staff => staff.PinStatus).IsRequired().HasMaxLength(50);
            entity.Property(staff => staff.LastActivitySummary).IsRequired().HasMaxLength(500);
            entity.HasIndex(staff => new { staff.BusinessProfileId, staff.PinStatus });
            entity.HasIndex(staff => staff.UserId);
            entity.HasOne(staff => staff.BusinessProfile)
                .WithMany(business => business.StaffAccess)
                .HasForeignKey(staff => staff.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(staff => staff.User)
                .WithMany()
                .HasForeignKey(staff => staff.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoyaltyTransactionEntity>(entity =>
        {
            entity.ToTable("LoyaltyTransactions");
            entity.HasKey(transaction => transaction.Id);
            entity.Property(transaction => transaction.TransactionType).IsRequired().HasMaxLength(50);
            entity.Property(transaction => transaction.PurchaseAmount).HasPrecision(18, 2);
            entity.Property(transaction => transaction.RedeemedValue).HasPrecision(18, 2);
            entity.Property(transaction => transaction.Status).IsRequired().HasMaxLength(50);
            entity.Property(transaction => transaction.Reference).IsRequired().HasMaxLength(80);
            entity.Property(transaction => transaction.Notes).HasMaxLength(1000);
            entity.Property(transaction => transaction.HeldReason).HasMaxLength(1000);
            entity.Property(transaction => transaction.ReviewedByUserId).HasMaxLength(450);
            entity.HasIndex(transaction => transaction.Reference).IsUnique();
            entity.HasIndex(transaction => new { transaction.MemberProfileId, transaction.CreatedUtc });
            entity.HasIndex(transaction => new { transaction.BusinessProfileId, transaction.CreatedUtc });
            entity.HasIndex(transaction => new { transaction.Status, transaction.CreatedUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_LoyaltyTransactions_Points_Positive", "[Points] > 0"));
            entity.ToTable(table => table.HasCheckConstraint("CK_LoyaltyTransactions_Money_NonNegative", "[PurchaseAmount] >= 0 AND [RedeemedValue] >= 0"));
            entity.HasOne(transaction => transaction.MemberProfile)
                .WithMany(member => member.LoyaltyTransactions)
                .HasForeignKey(transaction => transaction.MemberProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.BusinessProfile)
                .WithMany(business => business.LoyaltyTransactions)
                .HasForeignKey(transaction => transaction.BusinessProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.BusinessLocation)
                .WithMany(location => location.LoyaltyTransactions)
                .HasForeignKey(transaction => transaction.BusinessLocationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.StaffAccess)
                .WithMany(staff => staff.LoyaltyTransactions)
                .HasForeignKey(transaction => transaction.StaffAccessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(transaction => transaction.ReviewedByUser)
                .WithMany()
                .HasForeignKey(transaction => transaction.ReviewedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<LoyaltyPointsRuleEntity>(entity =>
        {
            entity.ToTable("LoyaltyPointsRules");
            entity.HasKey(rule => rule.Id);
            entity.Property(rule => rule.Name).IsRequired().HasMaxLength(200);
            entity.Property(rule => rule.MaxRedemptionPercentage).HasPrecision(5, 2);
            entity.Property(rule => rule.CreatedByUserId).HasMaxLength(450);
            entity.HasIndex(rule => new { rule.IsActive, rule.EffectiveFromUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_LoyaltyPointsRules_Positive", "[EarnPointsPerPound] > 0 AND [PencePerPoint] > 0 AND [MaxRedemptionPercentage] > 0"));
            entity.HasOne(rule => rule.CreatedByUser)
                .WithMany()
                .HasForeignKey(rule => rule.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<BusinessAlertEntity>(entity =>
        {
            entity.ToTable("BusinessAlerts");
            entity.HasKey(alert => alert.Id);
            entity.Property(alert => alert.Title).IsRequired().HasMaxLength(200);
            entity.Property(alert => alert.Message).IsRequired().HasMaxLength(1000);
            entity.Property(alert => alert.Tone).IsRequired().HasMaxLength(50);
            entity.Property(alert => alert.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(alert => new { alert.BusinessProfileId, alert.Status, alert.CreatedUtc });
            entity.HasOne(alert => alert.BusinessProfile)
                .WithMany(business => business.Alerts)
                .HasForeignKey(alert => alert.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BusinessPayoutPeriodEntity>(entity =>
        {
            entity.ToTable("BusinessPayoutPeriods");
            entity.HasKey(payout => payout.Id);
            entity.Property(payout => payout.RedeemedAmount).HasPrecision(18, 2);
            entity.Property(payout => payout.Fees).HasPrecision(18, 2);
            entity.Property(payout => payout.PayoutTotal).HasPrecision(18, 2);
            entity.Property(payout => payout.Status).IsRequired().HasMaxLength(50);
            entity.Property(payout => payout.HeldReason).HasMaxLength(1000);
            entity.HasIndex(payout => new { payout.BusinessProfileId, payout.PeriodStartUtc });
            entity.HasIndex(payout => new { payout.Status, payout.CreatedUtc });
            entity.ToTable(table => table.HasCheckConstraint("CK_BusinessPayoutPeriods_Money_NonNegative", "[RedeemedAmount] >= 0 AND [Fees] >= 0 AND [PayoutTotal] >= 0"));
            entity.HasOne(payout => payout.BusinessProfile)
                .WithMany(business => business.PayoutPeriods)
                .HasForeignKey(payout => payout.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SecurityAuditEntryEntity>(entity =>
        {
            entity.ToTable("SecurityAuditEntries");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.ActorUserId).HasMaxLength(450);
            entity.Property(audit => audit.Title).IsRequired().HasMaxLength(200);
            entity.Property(audit => audit.Detail).IsRequired().HasMaxLength(1000);
            entity.Property(audit => audit.Status).IsRequired().HasMaxLength(50);
            entity.HasIndex(audit => new { audit.BusinessProfileId, audit.CreatedUtc });
            entity.HasOne(audit => audit.BusinessProfile)
                .WithMany(business => business.SecurityAuditEntries)
                .HasForeignKey(audit => audit.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(audit => audit.StaffAccess)
                .WithMany(staff => staff.SecurityAuditEntries)
                .HasForeignKey(audit => audit.StaffAccessId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(audit => audit.ActorUser)
                .WithMany()
                .HasForeignKey(audit => audit.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdminReviewCaseEntity>(entity =>
        {
            entity.ToTable("AdminReviewCases");
            entity.HasKey(reviewCase => reviewCase.Id);
            entity.Property(reviewCase => reviewCase.CaseType).IsRequired().HasMaxLength(50);
            entity.Property(reviewCase => reviewCase.Status).IsRequired().HasMaxLength(50);
            entity.Property(reviewCase => reviewCase.Priority).IsRequired().HasMaxLength(50);
            entity.Property(reviewCase => reviewCase.Title).IsRequired().HasMaxLength(200);
            entity.Property(reviewCase => reviewCase.Description).IsRequired().HasMaxLength(2000);
            entity.Property(reviewCase => reviewCase.ResolutionNotes).HasMaxLength(2000);
            entity.Property(reviewCase => reviewCase.AssignedToUserId).HasMaxLength(450);
            entity.HasIndex(reviewCase => new { reviewCase.CaseType, reviewCase.Status, reviewCase.Priority });
            entity.HasIndex(reviewCase => reviewCase.CreatedUtc);
            entity.HasOne(reviewCase => reviewCase.MemberProfile)
                .WithMany(member => member.AdminReviewCases)
                .HasForeignKey(reviewCase => reviewCase.MemberProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reviewCase => reviewCase.BusinessProfile)
                .WithMany(business => business.AdminReviewCases)
                .HasForeignKey(reviewCase => reviewCase.BusinessProfileId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reviewCase => reviewCase.LoyaltyTransaction)
                .WithMany(transaction => transaction.AdminReviewCases)
                .HasForeignKey(reviewCase => reviewCase.LoyaltyTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reviewCase => reviewCase.BusinessPayoutPeriod)
                .WithMany(payout => payout.AdminReviewCases)
                .HasForeignKey(reviewCase => reviewCase.BusinessPayoutPeriodId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(reviewCase => reviewCase.AssignedToUser)
                .WithMany()
                .HasForeignKey(reviewCase => reviewCase.AssignedToUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<AdminAuditLogEntity>(entity =>
        {
            entity.ToTable("AdminAuditLogs");
            entity.HasKey(audit => audit.Id);
            entity.Property(audit => audit.ActorUserId).HasMaxLength(450);
            entity.Property(audit => audit.EntityType).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.Action).IsRequired().HasMaxLength(100);
            entity.Property(audit => audit.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(audit => audit.Reason).HasMaxLength(1000);
            entity.HasIndex(audit => new { audit.EntityType, audit.EntityId, audit.CreatedUtc });
            entity.HasIndex(audit => new { audit.ActorUserId, audit.CreatedUtc });
            entity.HasOne(audit => audit.ActorUser)
                .WithMany()
                .HasForeignKey(audit => audit.ActorUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<MemberShopPreferenceEntity>(entity =>
        {
            entity.ToTable("MemberShopPreferences");
            entity.HasKey(preference => preference.Id);
            entity.Property(preference => preference.Notes).HasMaxLength(500);
            entity.HasIndex(preference => new { preference.MemberProfileId, preference.SavedUtc });
            entity.HasIndex(preference => preference.BusinessProfileId);
            entity.HasIndex(preference => new { preference.MemberProfileId, preference.BusinessProfileId }).IsUnique(false);
            entity.HasOne(preference => preference.MemberProfile)
                .WithMany(member => member.ShopPreferences)
                .HasForeignKey(preference => preference.MemberProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(preference => preference.BusinessProfile)
                .WithMany(business => business.MemberPreferences)
                .HasForeignKey(preference => preference.BusinessProfileId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
