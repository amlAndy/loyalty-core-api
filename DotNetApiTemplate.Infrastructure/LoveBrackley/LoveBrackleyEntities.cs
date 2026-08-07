using DotNetApiTemplate.Infrastructure.Identity;

namespace DotNetApiTemplate.Infrastructure.LoveBrackley;

public sealed class MemberProfileEntity
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? HomeArea { get; set; }
    public string PublicMemberReference { get; set; } = string.Empty;
    public string CardCode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int PointsBalance { get; set; }
    public bool MarketingConsent { get; set; }
    public bool EmailUpdatesPreference { get; set; }
    public bool LocalOffersPreference { get; set; }
    public bool CardReminderPreference { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<LoyaltyCardEntity> LoyaltyCards { get; set; } = [];
    public ICollection<LoyaltyTransactionEntity> LoyaltyTransactions { get; set; } = [];
    public ICollection<MemberShopPreferenceEntity> ShopPreferences { get; set; } = [];
    public ICollection<AdminReviewCaseEntity> AdminReviewCases { get; set; } = [];
}

public sealed class LoyaltyCardEntity
{
    public Guid Id { get; set; }
    public Guid MemberProfileId { get; set; }
    public string CardCode { get; set; } = string.Empty;
    public string PublicCardReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTimeOffset IssuedUtc { get; set; }
    public DateTimeOffset? LinkedUtc { get; set; }
    public DateTimeOffset? DisabledUtc { get; set; }
    public MemberProfileEntity? MemberProfile { get; set; }
}

public sealed class TownEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? PrimaryDomain { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? UpdatedUtc { get; set; }
    public ICollection<BusinessProfileEntity> BusinessProfiles { get; set; } = [];
}

public sealed class BusinessCategoryEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? UpdatedUtc { get; set; }
    public ICollection<BusinessProfileEntity> BusinessProfiles { get; set; } = [];
}

public sealed class BusinessProfileEntity
{
    public Guid Id { get; set; }
    public string OwnerUserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BrandReference { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Guid TownId { get; set; }
    public Guid BusinessCategoryId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string Town { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? SupportEmail { get; set; }
    public string PrimaryContactName { get; set; } = string.Empty;
    public string TradingHoursSummary { get; set; } = string.Empty;
    public string ParticipationStatus { get; set; } = string.Empty;
    public bool Featured { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public ApplicationUser? OwnerUser { get; set; }
    public TownEntity? TownLookup { get; set; }
    public BusinessCategoryEntity? BusinessCategory { get; set; }
    public ICollection<BusinessLocationEntity> Locations { get; set; } = [];
    public ICollection<BusinessStaffAccessEntity> StaffAccess { get; set; } = [];
    public ICollection<LoyaltyTransactionEntity> LoyaltyTransactions { get; set; } = [];
    public ICollection<BusinessAlertEntity> Alerts { get; set; } = [];
    public ICollection<BusinessPayoutPeriodEntity> PayoutPeriods { get; set; } = [];
    public ICollection<SecurityAuditEntryEntity> SecurityAuditEntries { get; set; } = [];
    public ICollection<MemberShopPreferenceEntity> MemberPreferences { get; set; } = [];
    public ICollection<AdminReviewCaseEntity> AdminReviewCases { get; set; } = [];
}

public sealed class BusinessLocationEntity
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string? AddressLine2 { get; set; }
    public string Town { get; set; } = string.Empty;
    public string Postcode { get; set; } = string.Empty;
    public string TillReference { get; set; } = string.Empty;
    public string TradingHoursSummary { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public ICollection<LoyaltyTransactionEntity> LoyaltyTransactions { get; set; } = [];
}

public sealed class BusinessStaffAccessEntity
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public string? UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string BusinessAccessRole { get; set; } = string.Empty;
    public string PinHash { get; set; } = string.Empty;
    public string PinStatus { get; set; } = string.Empty;
    public string LastActivitySummary { get; set; } = string.Empty;
    public DateTimeOffset? LastActivityUtc { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public ApplicationUser? User { get; set; }
    public ICollection<LoyaltyTransactionEntity> LoyaltyTransactions { get; set; } = [];
    public ICollection<SecurityAuditEntryEntity> SecurityAuditEntries { get; set; } = [];
}

public sealed class LoyaltyTransactionEntity
{
    public Guid Id { get; set; }
    public Guid MemberProfileId { get; set; }
    public Guid BusinessProfileId { get; set; }
    public Guid? BusinessLocationId { get; set; }
    public Guid? StaffAccessId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public int Points { get; set; }
    public decimal PurchaseAmount { get; set; }
    public decimal RedeemedValue { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reference { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? CompletedUtc { get; set; }
    public string? HeldReason { get; set; }
    public string? ReviewedByUserId { get; set; }
    public DateTimeOffset? ReviewedUtc { get; set; }
    public MemberProfileEntity? MemberProfile { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public BusinessLocationEntity? BusinessLocation { get; set; }
    public BusinessStaffAccessEntity? StaffAccess { get; set; }
    public ApplicationUser? ReviewedByUser { get; set; }
    public ICollection<AdminReviewCaseEntity> AdminReviewCases { get; set; } = [];
}

public sealed class LoyaltyPointsRuleEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EarnPointsPerPound { get; set; }
    public int PencePerPoint { get; set; }
    public decimal MaxRedemptionPercentage { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public string? CreatedByUserId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public ApplicationUser? CreatedByUser { get; set; }
}

public sealed class BusinessAlertEntity
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Tone { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ResolvedUtc { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
}

public sealed class BusinessPayoutPeriodEntity
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public DateTimeOffset PeriodStartUtc { get; set; }
    public DateTimeOffset PeriodEndUtc { get; set; }
    public decimal RedeemedAmount { get; set; }
    public decimal Fees { get; set; }
    public decimal PayoutTotal { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? PaidUtc { get; set; }
    public string? HeldReason { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public ICollection<AdminReviewCaseEntity> AdminReviewCases { get; set; } = [];
}

public sealed class SecurityAuditEntryEntity
{
    public Guid Id { get; set; }
    public Guid BusinessProfileId { get; set; }
    public Guid? StaffAccessId { get; set; }
    public string? ActorUserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Detail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ReviewedUtc { get; set; }
    public DateTimeOffset? EscalatedUtc { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public BusinessStaffAccessEntity? StaffAccess { get; set; }
    public ApplicationUser? ActorUser { get; set; }
}

public sealed class AdminReviewCaseEntity
{
    public Guid Id { get; set; }
    public string CaseType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public Guid? MemberProfileId { get; set; }
    public Guid? BusinessProfileId { get; set; }
    public Guid? LoyaltyTransactionId { get; set; }
    public Guid? BusinessPayoutPeriodId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? ResolutionNotes { get; set; }
    public string? AssignedToUserId { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ResolvedUtc { get; set; }
    public MemberProfileEntity? MemberProfile { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
    public LoyaltyTransactionEntity? LoyaltyTransaction { get; set; }
    public BusinessPayoutPeriodEntity? BusinessPayoutPeriod { get; set; }
    public ApplicationUser? AssignedToUser { get; set; }
}

public sealed class AdminAuditLogEntity
{
    public Guid Id { get; set; }
    public string? ActorUserId { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string? BeforeJson { get; set; }
    public string? AfterJson { get; set; }
    public string? Reason { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public ApplicationUser? ActorUser { get; set; }
}

public sealed class MemberShopPreferenceEntity
{
    public Guid Id { get; set; }
    public Guid MemberProfileId { get; set; }
    public Guid BusinessProfileId { get; set; }
    public bool ReceiveOfferNotifications { get; set; } = true;
    public bool ReceiveUpdateNotifications { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public DateTimeOffset SavedUtc { get; set; }
    public DateTimeOffset? RemovedUtc { get; set; }
    public MemberProfileEntity? MemberProfile { get; set; }
    public BusinessProfileEntity? BusinessProfile { get; set; }
}
