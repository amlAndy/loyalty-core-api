using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;

namespace DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;

public interface IMemberLoyaltyService
{
    Task<MemberDashboardDto?> GetDashboardAsync(string? userId, CancellationToken cancellationToken);
    Task<MemberWalletDto?> GetWalletAsync(string? userId, CancellationToken cancellationToken);
    Task<MemberActivityResponse?> GetActivityAsync(string? userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<MemberCardDto?> GetCardAsync(string? userId, CancellationToken cancellationToken);
    Task<MemberSettingsDto?> GetSettingsAsync(string? userId, CancellationToken cancellationToken);
    Task<MemberSettingsDto?> UpdateSettingsAsync(string? userId, UpdateMemberSettingsRequest request, CancellationToken cancellationToken);
    Task<FavoritedBusinessDto?> SaveFavoriteAsync(string? userId, Guid businessId, SaveMemberFavoriteRequest request, CancellationToken cancellationToken);
    Task<bool> RemoveFavoriteAsync(string? userId, Guid businessId, CancellationToken cancellationToken);
    Task<MemberFavoritesResponse?> GetFavoritesAsync(string? userId, int page, int pageSize, CancellationToken cancellationToken);
    Task<FavoritedBusinessDto?> UpdateFavoritePreferencesAsync(string? userId, Guid businessId, UpdateMemberFavoritePreferencesRequest request, CancellationToken cancellationToken);
}

public interface ILoveBrackleyLookupService
{
    Task<IReadOnlyList<TownSummaryDto>> GetTownsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BusinessCategoryDto>> GetBusinessCategoriesAsync(CancellationToken cancellationToken);
}

public interface IParticipatingBusinessService
{
    Task<PagedResponse<ParticipatingBusinessSummaryDto>> SearchAsync(string? category, string? townSlug, string? searchTerm, bool? featured, int page, int pageSize, CancellationToken cancellationToken);
}

public interface IBusinessLoyaltyService
{
    Task<BusinessDashboardDto?> GetDashboardAsync(Guid businessId, CancellationToken cancellationToken);
    Task<BusinessMemberSearchResponse?> SearchMembersAsync(Guid businessId, string? searchTerm, string? activityStatus, int page, int pageSize, CancellationToken cancellationToken);
    Task<BusinessMemberDetailDto?> GetMemberAsync(Guid businessId, Guid memberId, CancellationToken cancellationToken);
    Task<BusinessMemberDetailDto?> GetMemberByCardAsync(Guid businessId, string cardCode, CancellationToken cancellationToken);
    Task<LoyaltyTransactionDto?> EarnAsync(Guid businessId, EarnPointsRequest request, CancellationToken cancellationToken);
    Task<LoyaltyTransactionDto?> RedeemAsync(Guid businessId, RedeemPointsRequest request, string? actorUserId, string? activeRoleId, CancellationToken cancellationToken);
    Task<BusinessTransactionSearchResponse?> SearchTransactionsAsync(Guid businessId, string? searchTerm, string? dateRange, string? transactionType, Guid? staffMemberId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BusinessStaffAccessOverviewDto?> GetStaffAccessOverviewAsync(Guid businessId, CancellationToken cancellationToken);
    Task<PagedResponse<BusinessStaffAccessRecordDto>?> SearchStaffAccessAsync(Guid businessId, string? accessStatus, int page, int pageSize, CancellationToken cancellationToken);
    Task<BusinessStaffAccessRecordDto?> CreateStaffAccessAsync(Guid businessId, CreateBusinessStaffAccessRequest request, CancellationToken cancellationToken);
    Task<BusinessStaffAccessRecordDto?> ResetStaffPinAsync(Guid businessId, Guid staffAccessId, ResetStaffPinRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<BusinessPayoutOverviewDto?> GetPayoutOverviewAsync(Guid businessId, CancellationToken cancellationToken);
    Task<PagedResponse<BusinessPayoutDto>?> SearchPayoutsAsync(Guid businessId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BusinessSettingsDto?> GetSettingsAsync(Guid businessId, CancellationToken cancellationToken);
    Task<BusinessSettingsDto?> UpdateSettingsAsync(Guid businessId, UpdateBusinessSettingsRequest request, CancellationToken cancellationToken);
}

public interface IAdminLoveBrackleyService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken);
    Task<PagedResponse<AdminBusinessDto>> SearchBusinessesAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminBusinessDto?> CreateBusinessAsync(CreateAdminBusinessRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<BusinessSettingsDto?> GetBusinessAsync(Guid businessId, CancellationToken cancellationToken);
    Task<BusinessSettingsDto?> UpdateBusinessAsync(Guid businessId, UpdateBusinessSettingsRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<AdminBusinessDto?> SetBusinessStatusAsync(Guid businessId, string status, string action, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResponse<AdminMemberDto>> SearchMembersAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminMemberDto?> CreateMemberAsync(CreateAdminMemberRequest request, CancellationToken cancellationToken);
    Task<AdminMemberDetailDto?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken);
    Task<AdminMemberDetailDto?> UpdateMemberAsync(Guid memberId, UpdateMemberSettingsRequest request, CancellationToken cancellationToken);
    Task<AdminMemberDto?> SetMemberStatusAsync(Guid memberId, string status, CancellationToken cancellationToken);
    Task<LoyaltyTransactionDto?> AdjustPointsAsync(Guid memberId, AdjustPointsRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResponse<AdminReviewCaseDto>> SearchReviewsAsync(string? caseType, string? status, string? priority, int page, int pageSize, CancellationToken cancellationToken);
    Task<AdminReviewCaseDto?> GetReviewAsync(Guid caseId, CancellationToken cancellationToken);
    Task<AdminReviewCaseDto?> ResolveReviewAsync(Guid caseId, ResolveReviewCaseRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<AdminReviewCaseDto?> EscalateReviewAsync(Guid caseId, EscalateReviewCaseRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResponse<LoyaltyTransactionDto>> SearchTransactionsAsync(Guid? businessId, Guid? memberId, string? transactionType, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<PagedResponse<BusinessStaffAccessRecordDto>> SearchAccessAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<BusinessStaffAccessRecordDto?> CreateAccessProvisioningAsync(CreateBusinessStaffAccessProvisioningRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<BusinessStaffAccessRecordDto?> UpdateAccessProvisioningAsync(Guid staffAccessId, UpdateBusinessStaffAccessProvisioningRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResponse<BusinessPayoutDto>> SearchPayoutsAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken);
    Task<PointsRuleDto?> GetPointsRuleAsync(CancellationToken cancellationToken);
    Task<PointsRuleDto> UpdatePointsRuleAsync(UpdatePointsRuleRequest request, string? actorUserId, CancellationToken cancellationToken);
    Task<PagedResponse<AdminAuditLogDto>> SearchAuditAsync(string? entityType, string? entityId, string? actorUserId, int page, int pageSize, CancellationToken cancellationToken);
}
