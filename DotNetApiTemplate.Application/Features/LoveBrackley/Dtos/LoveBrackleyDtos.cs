namespace DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;

public sealed record DataResponse<T>(T Data);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);

public sealed record TownSummaryDto(
    Guid Id,
    string Name,
    string Slug,
    string DisplayName,
    string? PrimaryDomain);

public sealed record BusinessCategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description);

public sealed record MemberDashboardDto(
    string DisplayName,
    string PublicMemberReference,
    int PointsBalance,
    decimal EstimatedRewardValue,
    int TransactionsThisMonth,
    IReadOnlyList<string> Highlights,
    IReadOnlyList<MemberActivityEntryDto> RecentActivity);

public sealed record MemberWalletDto(
    int PointsBalance,
    decimal RewardValue,
    string MilestoneMessage,
    IReadOnlyList<string> QuickLinks,
    string RedemptionGuidance,
    IReadOnlyList<LoyaltyCardSummaryDto> Cards);

public sealed record LoyaltyCardSummaryDto(
    Guid Id,
    string CardCode,
    string PublicCardReference,
    string Status,
    bool IsPrimary);

public sealed record MemberActivityEntryDto(
    Guid Id,
    string TransactionType,
    string BusinessName,
    int Points,
    decimal PurchaseAmount,
    decimal RedeemedValue,
    string Status,
    string Reference,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc);

public sealed record MemberActivityResponse(
    IReadOnlyList<MemberActivityEntryDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int EarnedPoints,
    int RedeemedPoints,
    decimal RedeemedValue);

public sealed record MemberCardDto(
    string MemberName,
    string MembershipNumber,
    string CardCode,
    int PointsTotal,
    string Status,
    string CardNote);

public sealed record MemberSettingsDto(
    Guid MemberId,
    string DisplayName,
    string? Email,
    string? MobileNumber,
    string? HomeArea,
    bool MarketingConsent,
    bool EmailUpdatesPreference,
    bool LocalOffersPreference,
    bool CardReminderPreference);

public sealed record UpdateMemberSettingsRequest(
    string DisplayName,
    string? MobileNumber,
    string? HomeArea,
    bool MarketingConsent,
    bool EmailUpdatesPreference,
    bool LocalOffersPreference,
    bool CardReminderPreference);

public sealed record ParticipatingBusinessSummaryDto(
    Guid BusinessId,
    string Name,
    string Category,
    string Description,
    string LocationName,
    string Town,
    Guid TownId,
    string TownName,
    string TownSlug,
    Guid BusinessCategoryId,
    string CategoryName,
    string CategorySlug,
    bool Featured,
    string TradingHoursSummary);

public sealed record BusinessDashboardDto(
    Guid BusinessId,
    string BusinessName,
    IReadOnlyList<BusinessDashboardMetricDto> Metrics,
    IReadOnlyList<BusinessAlertDto> Alerts,
    IReadOnlyList<BusinessRecentTransactionDto> RecentTransactions,
    IReadOnlyList<string> QuickActions);

public sealed record BusinessDashboardMetricDto(string Label, string Value, string Tone);

public sealed record BusinessAlertDto(Guid Id, string Title, string Message, string Tone, string Status, DateTimeOffset CreatedUtc);

public sealed record BusinessRecentTransactionDto(
    Guid Id,
    string MemberName,
    string TransactionType,
    int Points,
    decimal PurchaseAmount,
    decimal RedeemedValue,
    string Status,
    DateTimeOffset CreatedUtc);

public sealed record BusinessMemberSummaryDto(
    string MemberId,
    string PublicMemberReference,
    string DisplayName,
    string? Email,
    string? MobileNumber,
    string Status,
    int PointsBalance,
    DateTimeOffset? LastActivityUtc,
    string ActivityStatus,
    string LastActivitySummary,
    string PreferredAction,
    string Notes);

public sealed record BusinessMemberSearchResponse(IReadOnlyList<BusinessMemberSummaryDto> Items, int Page, int PageSize, int TotalCount);

public sealed record BusinessMemberDetailDto(
    Guid MemberId,
    string PublicMemberReference,
    string DisplayName,
    string? Email,
    string? MobileNumber,
    string Status,
    int PointsBalance,
    IReadOnlyList<MemberActivityEntryDto> RecentActivity);

public sealed record EarnPointsRequest(Guid MemberId, Guid? StaffAccessId, Guid? BusinessLocationId, decimal PurchaseAmount, string? Notes);

public sealed record RedeemPointsRequest(Guid MemberId, Guid? BusinessLocationId, int Points, decimal PurchaseAmount, string? Notes);

public sealed record LoyaltyTransactionDto(
    Guid Id,
    Guid MemberId,
    string MemberName,
    Guid BusinessId,
    string BusinessName,
    Guid? StaffAccessId,
    string? StaffName,
    string TransactionType,
    int Points,
    decimal PurchaseAmount,
    decimal RedeemedValue,
    string Status,
    string Reference,
    string? Notes,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc,
    string? HeldReason);

public sealed record BusinessTransactionSearchResponse(IReadOnlyList<LoyaltyTransactionDto> Items, int Page, int PageSize, int TotalCount);

public sealed record BusinessStaffAccessOverviewDto(
    Guid BusinessId,
    int TotalStaff,
    int ActivePins,
    int ResetRequiredPins,
    int HeldPins,
    IReadOnlyList<BusinessDashboardMetricDto> RoleSummaries,
    IReadOnlyList<SecurityAuditEntryDto> SecurityAuditEntries);

public sealed record BusinessStaffAccessRecordDto(
    Guid Id,
    Guid BusinessId,
    string? UserId,
    string DisplayName,
    string BusinessAccessRole,
    string AccessStatus,
    string PinStatus,
    string LastActivitySummary,
    DateTimeOffset? LastActivityUtc,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record CreateBusinessStaffAccessRequest(string? UserId, string DisplayName, string BusinessAccessRole, string Pin);

public sealed record ResetStaffPinRequest(string NewPin, string? Reason);

public sealed record CreateBusinessStaffAccessProvisioningRequest(Guid BusinessId, string? UserId, string DisplayName, string BusinessAccessRole, string AccessStatus, string? Reason);

public sealed record UpdateBusinessStaffAccessProvisioningRequest(string? UserId, string AccessStatus, string? Reason);

public sealed record BusinessPayoutOverviewDto(Guid BusinessId, decimal PendingTotal, decimal PaidTotal, decimal HeldTotal, IReadOnlyList<BusinessPayoutDto> RecentPayouts);

public sealed record BusinessPayoutDto(
    Guid Id,
    Guid BusinessId,
    DateTimeOffset PeriodStartUtc,
    DateTimeOffset PeriodEndUtc,
    decimal RedeemedAmount,
    decimal Fees,
    decimal PayoutTotal,
    string Status,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? PaidUtc,
    string? HeldReason);

public sealed record BusinessSettingsDto(
    Guid BusinessId,
    string Name,
    string Category,
    Guid BusinessCategoryId,
    string CategoryName,
    string CategorySlug,
    string Description,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string Town,
    Guid TownId,
    string TownName,
    string TownSlug,
    string Postcode,
    string ContactEmail,
    string? SupportEmail,
    string PrimaryContactName,
    string TradingHoursSummary,
    string ParticipationStatus,
    bool Featured);

public sealed record UpdateBusinessSettingsRequest(
    string Name,
    string Category,
    string Description,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string Town,
    string Postcode,
    string ContactEmail,
    string? SupportEmail,
    string PrimaryContactName,
    string TradingHoursSummary);

public sealed record AdminUpdateBusinessSettingsRequest(
    string Name,
    string Category,
    string Description,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string Town,
    string Postcode,
    string ContactEmail,
    string? SupportEmail,
    string PrimaryContactName,
    string TradingHoursSummary,
    bool Featured);

public sealed record AdminDashboardDto(
    IReadOnlyList<BusinessDashboardMetricDto> Metrics,
    IReadOnlyList<AdminQueueItemDto> Queues,
    IReadOnlyList<string> Actions,
    IReadOnlyList<string> Notices);

public sealed record AdminQueueItemDto(string Label, int Count, string Tone);

public sealed record AdminBusinessDto(
    Guid Id,
    string Name,
    string BrandReference,
    string Category,
    Guid BusinessCategoryId,
    string CategoryName,
    string CategorySlug,
    Guid TownId,
    string TownName,
    string TownSlug,
    string ParticipationStatus,
    bool Featured,
    string OwnerUserId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record AdminMemberDto(
    Guid Id,
    string UserId,
    string PublicMemberReference,
    string DisplayName,
    string? Email,
    string Status,
    int PointsBalance,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record AdminMemberDetailDto(
    Guid Id,
    string UserId,
    string PublicMemberReference,
    string DisplayName,
    string? Email,
    string? MobileNumber,
    string? HomeArea,
    bool MarketingConsent,
    bool EmailUpdatesPreference,
    bool LocalOffersPreference,
    bool CardReminderPreference,
    string Status,
    int PointsBalance,
    DateTimeOffset CreatedUtc,
    DateTimeOffset UpdatedUtc);

public sealed record AdminReviewCaseDto(
    Guid Id,
    string CaseType,
    string Status,
    string Priority,
    Guid? MemberProfileId,
    Guid? BusinessProfileId,
    Guid? LoyaltyTransactionId,
    Guid? BusinessPayoutPeriodId,
    string Title,
    string Description,
    string? ResolutionNotes,
    string? AssignedToUserId,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? ResolvedUtc);

public sealed record SecurityAuditEntryDto(Guid Id, string Title, string Detail, string Status, DateTimeOffset CreatedUtc, DateTimeOffset? ReviewedUtc, DateTimeOffset? EscalatedUtc);

public sealed record AdminAuditLogDto(
    Guid Id,
    string? ActorUserId,
    string EntityType,
    string EntityId,
    string Action,
    string Summary,
    string? BeforeJson,
    string? AfterJson,
    string? Reason,
    DateTimeOffset CreatedUtc);

public sealed record PointsRuleDto(
    Guid Id,
    string Name,
    int EarnPointsPerPound,
    int PencePerPoint,
    decimal MaxRedemptionPercentage,
    bool IsActive,
    DateTimeOffset EffectiveFromUtc,
    DateTimeOffset? EffectiveToUtc,
    string? CreatedByUserId,
    DateTimeOffset CreatedUtc);

public sealed record UpdatePointsRuleRequest(string Name, int EarnPointsPerPound, int PencePerPoint, decimal MaxRedemptionPercentage);

public sealed record CreateAdminBusinessRequest(
    string OwnerUserId,
    string Name,
    string BrandReference,
    string Category,
    string Description,
    string LocationName,
    string AddressLine1,
    string? AddressLine2,
    string Town,
    string Postcode,
    string ContactEmail,
    string? SupportEmail,
    string PrimaryContactName,
    string TradingHoursSummary,
    bool Featured);

public sealed record CreateAdminMemberRequest(
    string UserId,
    string DisplayName,
    string? Email,
    string? MobileNumber,
    string? HomeArea);

public sealed record AdjustPointsRequest(Guid BusinessId, int Points, string Reason);

public sealed record ResolveReviewCaseRequest(string ResolutionNotes);

public sealed record EscalateReviewCaseRequest(string? Reason);

public sealed record FavoritedBusinessDto(
    Guid PreferenceId,
    Guid BusinessId,
    string Name,
    string Category,
    Guid BusinessCategoryId,
    string CategoryName,
    string CategorySlug,
    string LocationName,
    string Town,
    Guid TownId,
    string TownName,
    string TownSlug,
    bool Featured,
    bool ReceiveOfferNotifications,
    bool ReceiveUpdateNotifications,
    string? Notes,
    DateTimeOffset SavedUtc);

public sealed record SaveMemberFavoriteRequest(string? Notes);

public sealed record UpdateMemberFavoritePreferencesRequest(
    bool ReceiveOfferNotifications,
    bool ReceiveUpdateNotifications,
    string? Notes);

public sealed record MemberFavoritesResponse(IReadOnlyList<FavoritedBusinessDto> Items, int Page, int PageSize, int TotalCount);
