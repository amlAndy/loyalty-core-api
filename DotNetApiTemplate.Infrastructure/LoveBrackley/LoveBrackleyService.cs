using System.Security.Cryptography;
using System.Text;
using DotNetApiTemplate.Application.Features.LoveBrackley.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Interfaces;
using DotNetApiTemplate.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.LoveBrackley;

public sealed class LoveBrackleyService : IMemberLoyaltyService, IParticipatingBusinessService, IBusinessLoyaltyService, IAdminLoveBrackleyService
{
    private const string ActiveStatus = "Active";
    private const string CompletedStatus = "Completed";
    private const string ReviewStatus = "Review";
    private const string HeldStatus = "Held";
    private const string EarnType = "Earn";
    private const string RedeemType = "Redeem";
    private const string ManualAdjustmentType = "ManualAdjustment";

    private readonly ApplicationDbContext _dbContext;

    public LoveBrackleyService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MemberDashboardDto?> GetDashboardAsync(string? userId, CancellationToken cancellationToken)
    {
        var member = await GetMemberForUserAsync(userId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        var monthStart = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var recentActivity = await MemberActivityQuery(member.Id)
            .OrderByDescending(transaction => transaction.CreatedUtc)
            .Take(5)
            .Select(transaction => ToMemberActivityEntry(transaction))
            .ToListAsync(cancellationToken);

        var transactionsThisMonth = await _dbContext.LoyaltyTransactions
            .CountAsync(transaction => transaction.MemberProfileId == member.Id && transaction.CreatedUtc >= monthStart, cancellationToken);

        return new MemberDashboardDto(
            member.DisplayName,
            member.PublicMemberReference,
            member.PointsBalance,
            PointsToPounds(member.PointsBalance),
            transactionsThisMonth,
            ["Earn 1 point for every £1 spent", "Redeem up to 20% off a local bill", "Use your card at participating Brackley businesses"],
            recentActivity);
    }

    public async Task<MemberWalletDto?> GetWalletAsync(string? userId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.MemberProfiles
            .Include(candidate => candidate.LoyaltyCards)
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (member is null)
        {
            return null;
        }

        var cards = member.LoyaltyCards
            .OrderByDescending(card => card.IsPrimary)
            .ThenBy(card => card.IssuedUtc)
            .Select(card => new LoyaltyCardSummaryDto(card.Id, card.CardCode, card.PublicCardReference, card.Status, card.IsPrimary))
            .ToList();

        return new MemberWalletDto(
            member.PointsBalance,
            PointsToPounds(member.PointsBalance),
            member.PointsBalance >= 500 ? "You have enough points for £5 off locally." : "Keep collecting points towards your next local reward.",
            ["View card", "Find participating businesses", "Review activity"],
            "100 points = £1. Redemptions can cover up to 20% of a bill.",
            cards);
    }

    public async Task<MemberActivityResponse?> GetActivityAsync(string? userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        var member = await GetMemberForUserAsync(userId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = MemberActivityQuery(member.Id);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(transaction => transaction.CreatedUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(transaction => ToMemberActivityEntry(transaction))
            .ToListAsync(cancellationToken);

        var completed = _dbContext.LoyaltyTransactions
            .Where(transaction => transaction.MemberProfileId == member.Id && transaction.Status == CompletedStatus);
        var earned = await completed.Where(transaction => transaction.TransactionType == EarnType).SumAsync(transaction => transaction.Points, cancellationToken);
        var redeemed = await completed.Where(transaction => transaction.TransactionType == RedeemType).SumAsync(transaction => transaction.Points, cancellationToken);
        var redeemedValue = await completed.Where(transaction => transaction.TransactionType == RedeemType).SumAsync(transaction => transaction.RedeemedValue, cancellationToken);

        return new MemberActivityResponse(items, page, pageSize, totalCount, earned, redeemed, redeemedValue);
    }

    public async Task<MemberCardDto?> GetCardAsync(string? userId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.MemberProfiles
            .Include(candidate => candidate.LoyaltyCards)
            .SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);

        if (member is null)
        {
            return null;
        }

        var primaryCard = member.LoyaltyCards.FirstOrDefault(card => card.IsPrimary) ?? member.LoyaltyCards.FirstOrDefault();
        return new MemberCardDto(
            member.DisplayName,
            member.PublicMemberReference,
            primaryCard?.CardCode ?? member.CardCode,
            member.PointsBalance,
            member.Status,
            "Show this card when earning or redeeming points in participating Brackley businesses.");
    }

    public async Task<MemberSettingsDto?> GetSettingsAsync(string? userId, CancellationToken cancellationToken)
    {
        var member = await GetMemberForUserAsync(userId, cancellationToken);
        return member is null ? null : ToMemberSettings(member);
    }

    public async Task<MemberSettingsDto?> UpdateSettingsAsync(string? userId, UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        var member = await GetMemberForUserAsync(userId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName))
        {
            ThrowValidation(nameof(request.DisplayName), "Display name is required.");
        }

        member.DisplayName = request.DisplayName.Trim();
        member.MobileNumber = TrimToNull(request.MobileNumber);
        member.HomeArea = TrimToNull(request.HomeArea);
        member.MarketingConsent = request.MarketingConsent;
        member.EmailUpdatesPreference = request.EmailUpdatesPreference;
        member.LocalOffersPreference = request.LocalOffersPreference;
        member.CardReminderPreference = request.CardReminderPreference;
        member.UpdatedUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return ToMemberSettings(member);
    }

    public async Task<PagedResponse<ParticipatingBusinessSummaryDto>> SearchAsync(string? category, string? searchTerm, bool? featured, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.BusinessProfiles.AsNoTracking()
            .Where(business => business.ParticipationStatus == ActiveStatus);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(business => business.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(business => business.Name.Contains(searchTerm) || business.Description.Contains(searchTerm) || business.LocationName.Contains(searchTerm));
        }

        if (featured.HasValue)
        {
            query = query.Where(business => business.Featured == featured.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(business => business.Featured)
            .ThenBy(business => business.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(business => new ParticipatingBusinessSummaryDto(
                business.Id,
                business.Name,
                business.Category,
                business.Description,
                business.LocationName,
                business.Town,
                business.Featured,
                business.TradingHoursSummary))
            .ToListAsync(cancellationToken);

        return new PagedResponse<ParticipatingBusinessSummaryDto>(items, page, pageSize, totalCount);
    }

    public async Task<BusinessDashboardDto?> GetDashboardAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _dbContext.BusinessProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var monthStart = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero);
        var transactions = _dbContext.LoyaltyTransactions.AsNoTracking().Where(transaction => transaction.BusinessProfileId == businessId);
        var pointsIssued = await transactions.Where(transaction => transaction.TransactionType == EarnType && transaction.Status == CompletedStatus).SumAsync(transaction => transaction.Points, cancellationToken);
        var pointsRedeemed = await transactions.Where(transaction => transaction.TransactionType == RedeemType && transaction.Status == CompletedStatus).SumAsync(transaction => transaction.Points, cancellationToken);
        var monthlyTransactions = await transactions.CountAsync(transaction => transaction.CreatedUtc >= monthStart, cancellationToken);
        var alerts = await _dbContext.BusinessAlerts.AsNoTracking()
            .Where(alert => alert.BusinessProfileId == businessId && alert.Status == "Open")
            .OrderByDescending(alert => alert.CreatedUtc)
            .Select(alert => new BusinessAlertDto(alert.Id, alert.Title, alert.Message, alert.Tone, alert.Status, alert.CreatedUtc))
            .ToListAsync(cancellationToken);
        var recentTransactions = await TransactionQuery()
            .Where(transaction => transaction.BusinessProfileId == businessId)
            .OrderByDescending(transaction => transaction.CreatedUtc)
            .Take(8)
            .Select(transaction => new BusinessRecentTransactionDto(
                transaction.Id,
                transaction.MemberProfile!.DisplayName,
                transaction.TransactionType,
                transaction.Points,
                transaction.PurchaseAmount,
                transaction.RedeemedValue,
                transaction.Status,
                transaction.CreatedUtc))
            .ToListAsync(cancellationToken);

        return new BusinessDashboardDto(
            businessId,
            business.Name,
            [
                new BusinessDashboardMetricDto("Points issued", pointsIssued.ToString(), "Info"),
                new BusinessDashboardMetricDto("Points redeemed", pointsRedeemed.ToString(), "Info"),
                new BusinessDashboardMetricDto("April transactions", monthlyTransactions.ToString(), "Info")
            ],
            alerts,
            recentTransactions,
            ["Look up member", "Issue points", "Redeem points", "Review transactions"]);
    }

    public async Task<BusinessMemberSearchResponse?> SearchMembersAsync(Guid businessId, string? searchTerm, string? activityStatus, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var interactedMemberIds = _dbContext.LoyaltyTransactions
            .Where(transaction => transaction.BusinessProfileId == businessId)
            .Select(transaction => transaction.MemberProfileId);
        var query = _dbContext.MemberProfiles.AsNoTracking()
            .Where(member => interactedMemberIds.Contains(member.Id) || member.Status == ActiveStatus);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(member => member.DisplayName.Contains(searchTerm) || member.PublicMemberReference.Contains(searchTerm) || member.CardCode.Contains(searchTerm));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var members = await query
            .OrderBy(member => member.DisplayName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var memberIds = members.Select(member => member.Id).ToArray();
        var lastActivities = await _dbContext.LoyaltyTransactions.AsNoTracking()
            .Where(transaction => transaction.BusinessProfileId == businessId && memberIds.Contains(transaction.MemberProfileId))
            .GroupBy(transaction => transaction.MemberProfileId)
            .Select(group => new
            {
                MemberId = group.Key,
                LastActivityUtc = group.Max(transaction => transaction.CreatedUtc)
            })
            .ToListAsync(cancellationToken);
        var lastActivityLookup = lastActivities.ToDictionary(activity => activity.MemberId, activity => activity.LastActivityUtc);

        var items = members.Select(member => ToBusinessMemberSummary(member, lastActivityLookup.GetValueOrDefault(member.Id))).ToList();
        if (!string.IsNullOrWhiteSpace(activityStatus))
        {
            items = items.Where(member => string.Equals(member.ActivityStatus, activityStatus, StringComparison.OrdinalIgnoreCase)).ToList();
            totalCount = items.Count;
        }

        return new BusinessMemberSearchResponse(items, page, pageSize, totalCount);
    }

    public async Task<BusinessMemberDetailDto?> GetMemberAsync(Guid businessId, Guid memberId, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        var member = await _dbContext.MemberProfiles.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken);
        return member is null ? null : await ToBusinessMemberDetailAsync(businessId, member, cancellationToken);
    }

    public async Task<BusinessMemberDetailDto?> GetMemberByCardAsync(Guid businessId, string cardCode, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        var member = await _dbContext.MemberProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.CardCode == cardCode || candidate.LoyaltyCards.Any(card => card.CardCode == cardCode), cancellationToken);
        return member is null ? null : await ToBusinessMemberDetailAsync(businessId, member, cancellationToken);
    }

    public async Task<LoyaltyTransactionDto?> EarnAsync(Guid businessId, EarnPointsRequest request, CancellationToken cancellationToken)
    {
        if (request.PurchaseAmount <= 0)
        {
            ThrowValidation(nameof(request.PurchaseAmount), "Purchase amount must be greater than zero.");
        }

        var rule = await GetActiveRuleAsync(cancellationToken);
        var points = (int)Math.Floor(request.PurchaseAmount) * rule.EarnPointsPerPound;
        if (points <= 0)
        {
            ThrowValidation(nameof(request.PurchaseAmount), "Earned points must be greater than zero.");
        }

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var member = await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.Id == request.MemberId, cancellationToken);
        var business = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        if (member is null || business is null)
        {
            return null;
        }

        member.PointsBalance += points;
        member.UpdatedUtc = DateTimeOffset.UtcNow;
        var transaction = new LoyaltyTransactionEntity
        {
            Id = Guid.NewGuid(),
            MemberProfileId = member.Id,
            BusinessProfileId = businessId,
            BusinessLocationId = request.BusinessLocationId,
            StaffAccessId = request.StaffAccessId,
            TransactionType = EarnType,
            Points = points,
            PurchaseAmount = request.PurchaseAmount,
            RedeemedValue = 0,
            Status = CompletedStatus,
            Reference = CreateReference("EARN"),
            Notes = TrimToNull(request.Notes),
            CreatedUtc = DateTimeOffset.UtcNow,
            CompletedUtc = DateTimeOffset.UtcNow
        };
        _dbContext.LoyaltyTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return await GetTransactionDtoAsync(transaction.Id, cancellationToken);
    }

    public async Task<LoyaltyTransactionDto?> RedeemAsync(Guid businessId, RedeemPointsRequest request, CancellationToken cancellationToken)
    {
        if (request.Points <= 0)
        {
            ThrowValidation(nameof(request.Points), "Redemption points must be greater than zero.");
        }

        if (request.PurchaseAmount <= 0)
        {
            ThrowValidation(nameof(request.PurchaseAmount), "Purchase amount must be greater than zero.");
        }

        var rule = await GetActiveRuleAsync(cancellationToken);
        var redeemedValue = PointsToPounds(request.Points, rule.PencePerPoint);
        var maxRedemption = request.PurchaseAmount * (rule.MaxRedemptionPercentage / 100m);
        if (redeemedValue > maxRedemption)
        {
            ThrowValidation(nameof(request.Points), $"Redemption cannot exceed {rule.MaxRedemptionPercentage:0}% of the bill.");
        }

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var member = await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.Id == request.MemberId, cancellationToken);
        var business = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        var staff = await _dbContext.BusinessStaffAccesses.SingleOrDefaultAsync(candidate => candidate.Id == request.StaffAccessId && candidate.BusinessProfileId == businessId, cancellationToken);
        if (member is null || business is null || staff is null)
        {
            return null;
        }

        if (member.PointsBalance < request.Points)
        {
            ThrowValidation(nameof(request.Points), "Member does not have enough points.");
        }

        var status = staff.PinStatus == ActiveStatus ? CompletedStatus : HeldStatus;
        var completedUtc = status == CompletedStatus ? DateTimeOffset.UtcNow : (DateTimeOffset?)null;
        if (status == CompletedStatus)
        {
            member.PointsBalance -= request.Points;
            member.UpdatedUtc = DateTimeOffset.UtcNow;
        }

        var transaction = new LoyaltyTransactionEntity
        {
            Id = Guid.NewGuid(),
            MemberProfileId = member.Id,
            BusinessProfileId = businessId,
            BusinessLocationId = request.BusinessLocationId,
            StaffAccessId = staff.Id,
            TransactionType = RedeemType,
            Points = request.Points,
            PurchaseAmount = request.PurchaseAmount,
            RedeemedValue = redeemedValue,
            Status = status,
            Reference = CreateReference("RED"),
            Notes = TrimToNull(request.Notes),
            HeldReason = status == HeldStatus ? "Staff PIN is not active." : null,
            CreatedUtc = DateTimeOffset.UtcNow,
            CompletedUtc = completedUtc
        };
        _dbContext.LoyaltyTransactions.Add(transaction);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);

        return await GetTransactionDtoAsync(transaction.Id, cancellationToken);
    }

    public async Task<BusinessTransactionSearchResponse?> SearchTransactionsAsync(Guid businessId, string? searchTerm, string? dateRange, string? transactionType, Guid? staffMemberId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = FilterTransactions(TransactionQuery().Where(transaction => transaction.BusinessProfileId == businessId), searchTerm, transactionType, status, staffMemberId, null, dateRange);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(transaction => transaction.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(transaction => ToTransactionDto(transaction)).ToListAsync(cancellationToken);
        return new BusinessTransactionSearchResponse(items, page, pageSize, totalCount);
    }

    public async Task<BusinessStaffAccessOverviewDto?> GetStaffAccessOverviewAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        var staff = await _dbContext.BusinessStaffAccesses.AsNoTracking().Where(candidate => candidate.BusinessProfileId == businessId).ToListAsync(cancellationToken);
        var security = await _dbContext.SecurityAuditEntries.AsNoTracking()
            .Where(entry => entry.BusinessProfileId == businessId)
            .OrderByDescending(entry => entry.CreatedUtc)
            .Take(10)
            .Select(entry => new SecurityAuditEntryDto(entry.Id, entry.Title, entry.Detail, entry.Status, entry.CreatedUtc, entry.ReviewedUtc, entry.EscalatedUtc))
            .ToListAsync(cancellationToken);

        return new BusinessStaffAccessOverviewDto(
            businessId,
            staff.Count,
            staff.Count(candidate => candidate.PinStatus == ActiveStatus),
            staff.Count(candidate => candidate.PinStatus == "ResetRequired"),
            staff.Count(candidate => candidate.PinStatus == HeldStatus),
            staff.GroupBy(candidate => candidate.BusinessAccessRole).Select(group => new BusinessDashboardMetricDto(group.Key, group.Count().ToString(), "Info")).ToList(),
            security);
    }

    public async Task<PagedResponse<BusinessStaffAccessRecordDto>?> SearchStaffAccessAsync(Guid businessId, string? pinStatus, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.BusinessStaffAccesses.AsNoTracking().Where(staff => staff.BusinessProfileId == businessId);
        if (!string.IsNullOrWhiteSpace(pinStatus))
        {
            query = query.Where(staff => staff.PinStatus == pinStatus);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(staff => staff.DisplayName).Skip((page - 1) * pageSize).Take(pageSize).Select(staff => ToStaffDto(staff)).ToListAsync(cancellationToken);
        return new PagedResponse<BusinessStaffAccessRecordDto>(items, page, pageSize, totalCount);
    }

    public async Task<BusinessStaffAccessRecordDto?> CreateStaffAccessAsync(Guid businessId, CreateBusinessStaffAccessRequest request, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        ValidatePin(request.Pin);
        var now = DateTimeOffset.UtcNow;
        var staff = new BusinessStaffAccessEntity
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = businessId,
            UserId = TrimToNull(request.UserId),
            DisplayName = request.DisplayName.Trim(),
            BusinessAccessRole = string.IsNullOrWhiteSpace(request.BusinessAccessRole) ? "Staff" : request.BusinessAccessRole.Trim(),
            PinHash = HashPin(request.Pin),
            PinStatus = ActiveStatus,
            LastActivitySummary = "Staff access created",
            CreatedUtc = now,
            UpdatedUtc = now
        };
        _dbContext.BusinessStaffAccesses.Add(staff);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStaffDto(staff);
    }

    public async Task<BusinessStaffAccessRecordDto?> ResetStaffPinAsync(Guid businessId, Guid staffAccessId, ResetStaffPinRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        ValidatePin(request.NewPin);
        var staff = await _dbContext.BusinessStaffAccesses.SingleOrDefaultAsync(candidate => candidate.Id == staffAccessId && candidate.BusinessProfileId == businessId, cancellationToken);
        if (staff is null)
        {
            return null;
        }

        staff.PinHash = HashPin(request.NewPin);
        staff.PinStatus = "ResetRequired";
        staff.LastActivitySummary = "PIN reset requested";
        staff.LastActivityUtc = DateTimeOffset.UtcNow;
        staff.UpdatedUtc = DateTimeOffset.UtcNow;
        _dbContext.SecurityAuditEntries.Add(new SecurityAuditEntryEntity
        {
            Id = Guid.NewGuid(),
            BusinessProfileId = businessId,
            StaffAccessId = staff.Id,
            ActorUserId = actorUserId,
            Title = "Staff PIN reset",
            Detail = TrimToNull(request.Reason) ?? "A staff PIN reset was requested.",
            Status = "Logged",
            CreatedUtc = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToStaffDto(staff);
    }

    public async Task<BusinessPayoutOverviewDto?> GetPayoutOverviewAsync(Guid businessId, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        var payouts = _dbContext.BusinessPayoutPeriods.AsNoTracking().Where(payout => payout.BusinessProfileId == businessId);
        var recent = await payouts.OrderByDescending(payout => payout.PeriodStartUtc).Take(6).Select(payout => ToPayoutDto(payout)).ToListAsync(cancellationToken);
        return new BusinessPayoutOverviewDto(
            businessId,
            await payouts.Where(payout => payout.Status == "Pending").SumAsync(payout => payout.PayoutTotal, cancellationToken),
            await payouts.Where(payout => payout.Status == "Paid").SumAsync(payout => payout.PayoutTotal, cancellationToken),
            await payouts.Where(payout => payout.Status == HeldStatus).SumAsync(payout => payout.PayoutTotal, cancellationToken),
            recent);
    }

    public async Task<PagedResponse<BusinessPayoutDto>?> SearchPayoutsAsync(Guid businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        if (!await BusinessExistsAsync(businessId, cancellationToken))
        {
            return null;
        }

        var response = await SearchPayoutsInternalAsync(businessId, status, page, pageSize, cancellationToken);
        return response;
    }

    public async Task<BusinessSettingsDto?> GetSettingsAsync(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _dbContext.BusinessProfiles.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        return business is null ? null : ToBusinessSettings(business);
    }

    public async Task<BusinessSettingsDto?> UpdateSettingsAsync(Guid businessId, UpdateBusinessSettingsRequest request, CancellationToken cancellationToken)
    {
        var business = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        ApplyBusinessSettings(business, request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToBusinessSettings(business);
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken)
    {
        var businessCount = await _dbContext.BusinessProfiles.CountAsync(cancellationToken);
        var memberCount = await _dbContext.MemberProfiles.CountAsync(cancellationToken);
        var reviewCount = await _dbContext.AdminReviewCases.CountAsync(reviewCase => reviewCase.Status != "Resolved" && reviewCase.Status != "Rejected", cancellationToken);
        var heldTransactions = await _dbContext.LoyaltyTransactions.CountAsync(transaction => transaction.Status == HeldStatus || transaction.Status == ReviewStatus, cancellationToken);
        return new AdminDashboardDto(
            [
                new BusinessDashboardMetricDto("Businesses", businessCount.ToString(), "Info"),
                new BusinessDashboardMetricDto("Members", memberCount.ToString(), "Info"),
                new BusinessDashboardMetricDto("Open reviews", reviewCount.ToString(), "Warning"),
                new BusinessDashboardMetricDto("Held transactions", heldTransactions.ToString(), "Warning")
            ],
            [
                new AdminQueueItemDto("Business approvals", await _dbContext.AdminReviewCases.CountAsync(reviewCase => reviewCase.CaseType == "BusinessApproval" && reviewCase.Status == "Open", cancellationToken), "Info"),
                new AdminQueueItemDto("Payout exceptions", await _dbContext.AdminReviewCases.CountAsync(reviewCase => reviewCase.CaseType == "PayoutException" && reviewCase.Status != "Resolved", cancellationToken), "Warning")
            ],
            ["Review businesses", "Review held transactions", "Inspect payouts", "Open audit log"],
            ["Development data is deterministic and safe to reseed."]);
    }

    public async Task<PagedResponse<AdminBusinessDto>> SearchBusinessesAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.BusinessProfiles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(business => business.Name.Contains(searchTerm) || business.BrandReference.Contains(searchTerm) || business.Category.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(business => business.ParticipationStatus == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(business => business.Name).Skip((page - 1) * pageSize).Take(pageSize).Select(business => ToAdminBusinessDto(business)).ToListAsync(cancellationToken);
        return new PagedResponse<AdminBusinessDto>(items, page, pageSize, totalCount);
    }

    public async Task<AdminBusinessDto?> CreateBusinessAsync(CreateAdminBusinessRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var business = new BusinessProfileEntity
        {
            Id = Guid.NewGuid(),
            OwnerUserId = request.OwnerUserId,
            BrandReference = request.BrandReference,
            ParticipationStatus = "PendingReview",
            CreatedUtc = now,
            UpdatedUtc = now
        };
        ApplyBusinessCreateFields(business, request);
        _dbContext.BusinessProfiles.Add(business);
        AddAdminAudit(actorUserId, "BusinessProfile", business.Id.ToString(), "Create", $"Created business {business.Name}.", null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminBusinessDto(business);
    }

    public async Task<BusinessSettingsDto?> GetBusinessAsync(Guid businessId, CancellationToken cancellationToken) => await GetSettingsAsync(businessId, cancellationToken);

    public async Task<BusinessSettingsDto?> UpdateBusinessAsync(Guid businessId, UpdateBusinessSettingsRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        var result = await UpdateSettingsAsync(businessId, request, cancellationToken);
        if (result is not null)
        {
            AddAdminAudit(actorUserId, "BusinessProfile", businessId.ToString(), "Update", $"Updated business {result.Name}.", null);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<AdminBusinessDto?> SetBusinessStatusAsync(Guid businessId, string status, string action, string? actorUserId, CancellationToken cancellationToken)
    {
        var business = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);
        if (business is null)
        {
            return null;
        }

        var before = business.ParticipationStatus;
        business.ParticipationStatus = status;
        business.UpdatedUtc = DateTimeOffset.UtcNow;
        AddAdminAudit(actorUserId, "BusinessProfile", business.Id.ToString(), action, $"Changed business status from {before} to {status}.", null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminBusinessDto(business);
    }

    public async Task<PagedResponse<AdminMemberDto>> SearchMembersAsync(string? searchTerm, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.MemberProfiles.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(member => member.DisplayName.Contains(searchTerm) || member.PublicMemberReference.Contains(searchTerm) || member.CardCode.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(member => member.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(member => member.DisplayName).Skip((page - 1) * pageSize).Take(pageSize).Select(member => ToAdminMemberDto(member)).ToListAsync(cancellationToken);
        return new PagedResponse<AdminMemberDto>(items, page, pageSize, totalCount);
    }

    public async Task<AdminMemberDto?> CreateMemberAsync(CreateAdminMemberRequest request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var member = new MemberProfileEntity
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            DisplayName = request.DisplayName.Trim(),
            Email = TrimToNull(request.Email),
            MobileNumber = TrimToNull(request.MobileNumber),
            HomeArea = TrimToNull(request.HomeArea),
            PublicMemberReference = $"LBM-{RandomNumberGenerator.GetInt32(100000, 999999)}",
            CardCode = $"CARD-{RandomNumberGenerator.GetInt32(100000, 999999)}",
            Status = ActiveStatus,
            PointsBalance = 0,
            MarketingConsent = true,
            EmailUpdatesPreference = true,
            LocalOffersPreference = true,
            CardReminderPreference = true,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        _dbContext.MemberProfiles.Add(member);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminMemberDto(member);
    }

    public async Task<AdminMemberDto?> GetMemberAsync(Guid memberId, CancellationToken cancellationToken)
    {
        var member = await _dbContext.MemberProfiles.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken);
        return member is null ? null : ToAdminMemberDto(member);
    }

    public async Task<AdminMemberDto?> UpdateMemberAsync(Guid memberId, UpdateMemberSettingsRequest request, CancellationToken cancellationToken)
    {
        var member = await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        member.DisplayName = request.DisplayName.Trim();
        member.MobileNumber = TrimToNull(request.MobileNumber);
        member.HomeArea = TrimToNull(request.HomeArea);
        member.MarketingConsent = request.MarketingConsent;
        member.EmailUpdatesPreference = request.EmailUpdatesPreference;
        member.LocalOffersPreference = request.LocalOffersPreference;
        member.CardReminderPreference = request.CardReminderPreference;
        member.UpdatedUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminMemberDto(member);
    }

    public async Task<AdminMemberDto?> SetMemberStatusAsync(Guid memberId, string status, CancellationToken cancellationToken)
    {
        var member = await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken);
        if (member is null)
        {
            return null;
        }

        member.Status = status;
        member.UpdatedUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToAdminMemberDto(member);
    }

    public async Task<LoyaltyTransactionDto?> AdjustPointsAsync(Guid memberId, AdjustPointsRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        if (request.Points == 0)
        {
            ThrowValidation(nameof(request.Points), "Adjustment points cannot be zero.");
        }

        await using var dbTransaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var member = await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.Id == memberId, cancellationToken);
        var business = await _dbContext.BusinessProfiles.SingleOrDefaultAsync(candidate => candidate.Id == request.BusinessId, cancellationToken);
        if (member is null || business is null)
        {
            return null;
        }

        var newBalance = member.PointsBalance + request.Points;
        if (newBalance < 0)
        {
            ThrowValidation(nameof(request.Points), "Points balance cannot go below zero.");
        }

        member.PointsBalance = newBalance;
        member.UpdatedUtc = DateTimeOffset.UtcNow;
        var transaction = new LoyaltyTransactionEntity
        {
            Id = Guid.NewGuid(),
            MemberProfileId = memberId,
            BusinessProfileId = request.BusinessId,
            TransactionType = ManualAdjustmentType,
            Points = Math.Abs(request.Points),
            PurchaseAmount = 0,
            RedeemedValue = 0,
            Status = CompletedStatus,
            Reference = CreateReference("ADJ"),
            Notes = request.Reason,
            CreatedUtc = DateTimeOffset.UtcNow,
            CompletedUtc = DateTimeOffset.UtcNow,
            ReviewedByUserId = actorUserId,
            ReviewedUtc = DateTimeOffset.UtcNow
        };
        _dbContext.LoyaltyTransactions.Add(transaction);
        AddAdminAudit(actorUserId, "MemberProfile", memberId.ToString(), "AdjustPoints", request.Reason, null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await dbTransaction.CommitAsync(cancellationToken);
        return await GetTransactionDtoAsync(transaction.Id, cancellationToken);
    }

    public async Task<PagedResponse<AdminReviewCaseDto>> SearchReviewsAsync(string? caseType, string? status, string? priority, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.AdminReviewCases.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(caseType)) query = query.Where(reviewCase => reviewCase.CaseType == caseType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(reviewCase => reviewCase.Status == status);
        if (!string.IsNullOrWhiteSpace(priority)) query = query.Where(reviewCase => reviewCase.Priority == priority);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(reviewCase => reviewCase.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(reviewCase => ToReviewDto(reviewCase)).ToListAsync(cancellationToken);
        return new PagedResponse<AdminReviewCaseDto>(items, page, pageSize, totalCount);
    }

    public async Task<AdminReviewCaseDto?> GetReviewAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var reviewCase = await _dbContext.AdminReviewCases.AsNoTracking().SingleOrDefaultAsync(candidate => candidate.Id == caseId, cancellationToken);
        return reviewCase is null ? null : ToReviewDto(reviewCase);
    }

    public async Task<AdminReviewCaseDto?> ResolveReviewAsync(Guid caseId, ResolveReviewCaseRequest request, CancellationToken cancellationToken)
    {
        var reviewCase = await _dbContext.AdminReviewCases.SingleOrDefaultAsync(candidate => candidate.Id == caseId, cancellationToken);
        if (reviewCase is null)
        {
            return null;
        }

        reviewCase.Status = "Resolved";
        reviewCase.ResolutionNotes = TrimToNull(request.ResolutionNotes);
        reviewCase.ResolvedUtc = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToReviewDto(reviewCase);
    }

    public async Task<AdminReviewCaseDto?> EscalateReviewAsync(Guid caseId, EscalateReviewCaseRequest request, CancellationToken cancellationToken)
    {
        var reviewCase = await _dbContext.AdminReviewCases.SingleOrDefaultAsync(candidate => candidate.Id == caseId, cancellationToken);
        if (reviewCase is null)
        {
            return null;
        }

        reviewCase.Status = "Escalated";
        reviewCase.ResolutionNotes = TrimToNull(request.Reason) ?? reviewCase.ResolutionNotes;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToReviewDto(reviewCase);
    }

    public async Task<PagedResponse<LoyaltyTransactionDto>> SearchTransactionsAsync(Guid? businessId, Guid? memberId, string? transactionType, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = FilterTransactions(TransactionQuery(), null, transactionType, status, null, memberId, null);
        if (businessId.HasValue)
        {
            query = query.Where(transaction => transaction.BusinessProfileId == businessId.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(transaction => transaction.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(transaction => ToTransactionDto(transaction)).ToListAsync(cancellationToken);
        return new PagedResponse<LoyaltyTransactionDto>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResponse<BusinessStaffAccessRecordDto>> SearchAccessAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.BusinessStaffAccesses.AsNoTracking().AsQueryable();
        if (businessId.HasValue) query = query.Where(staff => staff.BusinessProfileId == businessId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(staff => staff.PinStatus == status);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderBy(staff => staff.DisplayName).Skip((page - 1) * pageSize).Take(pageSize).Select(staff => ToStaffDto(staff)).ToListAsync(cancellationToken);
        return new PagedResponse<BusinessStaffAccessRecordDto>(items, page, pageSize, totalCount);
    }

    public async Task<PagedResponse<BusinessPayoutDto>> SearchPayoutsAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        return await SearchPayoutsInternalAsync(businessId, status, page, pageSize, cancellationToken);
    }

    public async Task<PointsRuleDto?> GetPointsRuleAsync(CancellationToken cancellationToken)
    {
        var rule = await _dbContext.LoyaltyPointsRules.AsNoTracking().OrderByDescending(candidate => candidate.IsActive).ThenByDescending(candidate => candidate.EffectiveFromUtc).FirstOrDefaultAsync(cancellationToken);
        return rule is null ? null : ToPointsRuleDto(rule);
    }

    public async Task<PointsRuleDto> UpdatePointsRuleAsync(UpdatePointsRuleRequest request, string? actorUserId, CancellationToken cancellationToken)
    {
        if (request.EarnPointsPerPound <= 0 || request.PencePerPoint <= 0 || request.MaxRedemptionPercentage <= 0)
        {
            ThrowValidation("pointsRule", "Points rule values must be greater than zero.");
        }

        var currentRules = await _dbContext.LoyaltyPointsRules.Where(rule => rule.IsActive).ToListAsync(cancellationToken);
        foreach (var currentRule in currentRules)
        {
            currentRule.IsActive = false;
            currentRule.EffectiveToUtc = DateTimeOffset.UtcNow;
        }

        var rule = new LoyaltyPointsRuleEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            EarnPointsPerPound = request.EarnPointsPerPound,
            PencePerPoint = request.PencePerPoint,
            MaxRedemptionPercentage = request.MaxRedemptionPercentage,
            IsActive = true,
            EffectiveFromUtc = DateTimeOffset.UtcNow,
            CreatedByUserId = actorUserId,
            CreatedUtc = DateTimeOffset.UtcNow
        };
        _dbContext.LoyaltyPointsRules.Add(rule);
        AddAdminAudit(actorUserId, "LoyaltyPointsRule", rule.Id.ToString(), "Update", "Updated active points rule.", null);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return ToPointsRuleDto(rule);
    }

    public async Task<PagedResponse<AdminAuditLogDto>> SearchAuditAsync(string? entityType, string? entityId, string? actorUserId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.AdminAuditLogs.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(audit => audit.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(entityId)) query = query.Where(audit => audit.EntityId == entityId);
        if (!string.IsNullOrWhiteSpace(actorUserId)) query = query.Where(audit => audit.ActorUserId == actorUserId);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(audit => audit.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(audit => ToAuditDto(audit)).ToListAsync(cancellationToken);
        return new PagedResponse<AdminAuditLogDto>(items, page, pageSize, totalCount);
    }

    private async Task<MemberProfileEntity?> GetMemberForUserAsync(string? userId, CancellationToken cancellationToken)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? null
            : await _dbContext.MemberProfiles.SingleOrDefaultAsync(candidate => candidate.UserId == userId, cancellationToken);
    }

    private IQueryable<LoyaltyTransactionEntity> MemberActivityQuery(Guid memberId)
    {
        return _dbContext.LoyaltyTransactions.AsNoTracking()
            .Include(transaction => transaction.BusinessProfile)
            .Where(transaction => transaction.MemberProfileId == memberId);
    }

    private IQueryable<LoyaltyTransactionEntity> TransactionQuery()
    {
        return _dbContext.LoyaltyTransactions.AsNoTracking()
            .Include(transaction => transaction.MemberProfile)
            .Include(transaction => transaction.BusinessProfile)
            .Include(transaction => transaction.StaffAccess);
    }

    private static IQueryable<LoyaltyTransactionEntity> FilterTransactions(IQueryable<LoyaltyTransactionEntity> query, string? searchTerm, string? transactionType, string? status, Guid? staffMemberId, Guid? memberId, string? dateRange)
    {
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(transaction => transaction.Reference.Contains(searchTerm) || transaction.MemberProfile!.DisplayName.Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(transactionType)) query = query.Where(transaction => transaction.TransactionType == transactionType);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(transaction => transaction.Status == status);
        if (staffMemberId.HasValue) query = query.Where(transaction => transaction.StaffAccessId == staffMemberId.Value);
        if (memberId.HasValue) query = query.Where(transaction => transaction.MemberProfileId == memberId.Value);
        if (string.Equals(dateRange, "month", StringComparison.OrdinalIgnoreCase)) query = query.Where(transaction => transaction.CreatedUtc >= DateTimeOffset.UtcNow.AddDays(-31));
        if (string.Equals(dateRange, "week", StringComparison.OrdinalIgnoreCase)) query = query.Where(transaction => transaction.CreatedUtc >= DateTimeOffset.UtcNow.AddDays(-7));
        return query;
    }

    private async Task<BusinessMemberDetailDto> ToBusinessMemberDetailAsync(Guid businessId, MemberProfileEntity member, CancellationToken cancellationToken)
    {
        var activity = await MemberActivityQuery(member.Id)
            .Where(transaction => transaction.BusinessProfileId == businessId)
            .OrderByDescending(transaction => transaction.CreatedUtc)
            .Take(10)
            .Select(transaction => ToMemberActivityEntry(transaction))
            .ToListAsync(cancellationToken);
        return new BusinessMemberDetailDto(member.Id, member.PublicMemberReference, member.DisplayName, member.Email, member.MobileNumber, member.Status, member.PointsBalance, activity);
    }

    private async Task<bool> BusinessExistsAsync(Guid businessId, CancellationToken cancellationToken)
    {
        return await _dbContext.BusinessProfiles.AnyAsync(candidate => candidate.Id == businessId, cancellationToken);
    }

    private async Task<LoyaltyPointsRuleEntity> GetActiveRuleAsync(CancellationToken cancellationToken)
    {
        var rule = await _dbContext.LoyaltyPointsRules.OrderByDescending(candidate => candidate.IsActive).ThenByDescending(candidate => candidate.EffectiveFromUtc).FirstOrDefaultAsync(cancellationToken);
        return rule ?? new LoyaltyPointsRuleEntity
        {
            Id = Guid.Empty,
            Name = "Default points rule",
            EarnPointsPerPound = 1,
            PencePerPoint = 1,
            MaxRedemptionPercentage = 20,
            IsActive = true,
            EffectiveFromUtc = DateTimeOffset.UtcNow,
            CreatedUtc = DateTimeOffset.UtcNow
        };
    }

    private async Task<LoyaltyTransactionDto?> GetTransactionDtoAsync(Guid transactionId, CancellationToken cancellationToken)
    {
        return await TransactionQuery().Where(transaction => transaction.Id == transactionId).Select(transaction => ToTransactionDto(transaction)).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<PagedResponse<BusinessPayoutDto>> SearchPayoutsInternalAsync(Guid? businessId, string? status, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = NormalizePage(page);
        pageSize = NormalizePageSize(pageSize);
        var query = _dbContext.BusinessPayoutPeriods.AsNoTracking().AsQueryable();
        if (businessId.HasValue) query = query.Where(payout => payout.BusinessProfileId == businessId.Value);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(payout => payout.Status == status);
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(payout => payout.PeriodStartUtc).Skip((page - 1) * pageSize).Take(pageSize).Select(payout => ToPayoutDto(payout)).ToListAsync(cancellationToken);
        return new PagedResponse<BusinessPayoutDto>(items, page, pageSize, totalCount);
    }

    private static MemberActivityEntryDto ToMemberActivityEntry(LoyaltyTransactionEntity transaction)
    {
        return new MemberActivityEntryDto(
            transaction.Id,
            transaction.TransactionType,
            transaction.BusinessProfile!.Name,
            transaction.Points,
            transaction.PurchaseAmount,
            transaction.RedeemedValue,
            transaction.Status,
            transaction.Reference,
            transaction.Notes,
            transaction.CreatedUtc,
            transaction.CompletedUtc);
    }

    private static LoyaltyTransactionDto ToTransactionDto(LoyaltyTransactionEntity transaction)
    {
        return new LoyaltyTransactionDto(
            transaction.Id,
            transaction.MemberProfileId,
            transaction.MemberProfile!.DisplayName,
            transaction.BusinessProfileId,
            transaction.BusinessProfile!.Name,
            transaction.StaffAccessId,
            transaction.StaffAccess == null ? null : transaction.StaffAccess.DisplayName,
            transaction.TransactionType,
            transaction.Points,
            transaction.PurchaseAmount,
            transaction.RedeemedValue,
            transaction.Status,
            transaction.Reference,
            transaction.Notes,
            transaction.CreatedUtc,
            transaction.CompletedUtc,
            transaction.HeldReason);
    }

    private static BusinessMemberSummaryDto ToBusinessMemberSummary(MemberProfileEntity member, DateTimeOffset? lastActivityUtc)
    {
        var activityStatus = member.Status == "Suspended"
            ? "Suspended"
            : lastActivityUtc is null
                ? "NeedsFollowUp"
                : lastActivityUtc < DateTimeOffset.UtcNow.AddDays(-45)
                    ? "Inactive"
                    : "Active";
        return new BusinessMemberSummaryDto(
            member.Id.ToString(),
            member.PublicMemberReference,
            member.DisplayName,
            member.Email,
            member.MobileNumber,
            member.Status,
            member.PointsBalance,
            lastActivityUtc,
            activityStatus,
            lastActivityUtc is null ? "No business activity yet" : "Recent loyalty activity recorded",
            activityStatus == "NeedsFollowUp" ? "Invite to earn points" : "Continue loyalty conversation",
            "Eligible for Love Brackley loyalty activity");
    }

    private static MemberSettingsDto ToMemberSettings(MemberProfileEntity member)
    {
        return new MemberSettingsDto(member.Id, member.DisplayName, member.Email, member.MobileNumber, member.HomeArea, member.MarketingConsent, member.EmailUpdatesPreference, member.LocalOffersPreference, member.CardReminderPreference);
    }

    private static BusinessStaffAccessRecordDto ToStaffDto(BusinessStaffAccessEntity staff)
    {
        return new BusinessStaffAccessRecordDto(staff.Id, staff.BusinessProfileId, staff.UserId, staff.DisplayName, staff.BusinessAccessRole, staff.PinStatus, staff.LastActivitySummary, staff.LastActivityUtc, staff.CreatedUtc, staff.UpdatedUtc);
    }

    private static BusinessPayoutDto ToPayoutDto(BusinessPayoutPeriodEntity payout)
    {
        return new BusinessPayoutDto(payout.Id, payout.BusinessProfileId, payout.PeriodStartUtc, payout.PeriodEndUtc, payout.RedeemedAmount, payout.Fees, payout.PayoutTotal, payout.Status, payout.CreatedUtc, payout.PaidUtc, payout.HeldReason);
    }

    private static BusinessSettingsDto ToBusinessSettings(BusinessProfileEntity business)
    {
        return new BusinessSettingsDto(business.Id, business.Name, business.Category, business.Description, business.LocationName, business.AddressLine1, business.AddressLine2, business.Town, business.Postcode, business.ContactEmail, business.SupportEmail, business.PrimaryContactName, business.TradingHoursSummary, business.ParticipationStatus, business.Featured);
    }

    private static AdminBusinessDto ToAdminBusinessDto(BusinessProfileEntity business)
    {
        return new AdminBusinessDto(business.Id, business.Name, business.BrandReference, business.Category, business.ParticipationStatus, business.Featured, business.OwnerUserId, business.CreatedUtc, business.UpdatedUtc);
    }

    private static AdminMemberDto ToAdminMemberDto(MemberProfileEntity member)
    {
        return new AdminMemberDto(member.Id, member.UserId, member.PublicMemberReference, member.DisplayName, member.Email, member.Status, member.PointsBalance, member.CreatedUtc, member.UpdatedUtc);
    }

    private static AdminReviewCaseDto ToReviewDto(AdminReviewCaseEntity reviewCase)
    {
        return new AdminReviewCaseDto(reviewCase.Id, reviewCase.CaseType, reviewCase.Status, reviewCase.Priority, reviewCase.MemberProfileId, reviewCase.BusinessProfileId, reviewCase.LoyaltyTransactionId, reviewCase.BusinessPayoutPeriodId, reviewCase.Title, reviewCase.Description, reviewCase.ResolutionNotes, reviewCase.AssignedToUserId, reviewCase.CreatedUtc, reviewCase.ResolvedUtc);
    }

    private static AdminAuditLogDto ToAuditDto(AdminAuditLogEntity audit)
    {
        return new AdminAuditLogDto(audit.Id, audit.ActorUserId, audit.EntityType, audit.EntityId, audit.Action, audit.Summary, audit.BeforeJson, audit.AfterJson, audit.Reason, audit.CreatedUtc);
    }

    private static PointsRuleDto ToPointsRuleDto(LoyaltyPointsRuleEntity rule)
    {
        return new PointsRuleDto(rule.Id, rule.Name, rule.EarnPointsPerPound, rule.PencePerPoint, rule.MaxRedemptionPercentage, rule.IsActive, rule.EffectiveFromUtc, rule.EffectiveToUtc, rule.CreatedByUserId, rule.CreatedUtc);
    }

    private static void ApplyBusinessSettings(BusinessProfileEntity business, UpdateBusinessSettingsRequest request)
    {
        business.Name = request.Name.Trim();
        business.Category = request.Category.Trim();
        business.Description = request.Description.Trim();
        business.LocationName = request.LocationName.Trim();
        business.AddressLine1 = request.AddressLine1.Trim();
        business.AddressLine2 = TrimToNull(request.AddressLine2);
        business.Town = request.Town.Trim();
        business.Postcode = request.Postcode.Trim();
        business.ContactEmail = request.ContactEmail.Trim();
        business.SupportEmail = TrimToNull(request.SupportEmail);
        business.PrimaryContactName = request.PrimaryContactName.Trim();
        business.TradingHoursSummary = request.TradingHoursSummary.Trim();
        business.Featured = request.Featured;
        business.UpdatedUtc = DateTimeOffset.UtcNow;
    }

    private static void ApplyBusinessCreateFields(BusinessProfileEntity business, CreateAdminBusinessRequest request)
    {
        business.Name = request.Name.Trim();
        business.Category = request.Category.Trim();
        business.Description = request.Description.Trim();
        business.LocationName = request.LocationName.Trim();
        business.AddressLine1 = request.AddressLine1.Trim();
        business.AddressLine2 = TrimToNull(request.AddressLine2);
        business.Town = request.Town.Trim();
        business.Postcode = request.Postcode.Trim();
        business.ContactEmail = request.ContactEmail.Trim();
        business.SupportEmail = TrimToNull(request.SupportEmail);
        business.PrimaryContactName = request.PrimaryContactName.Trim();
        business.TradingHoursSummary = request.TradingHoursSummary.Trim();
        business.Featured = request.Featured;
    }

    private void AddAdminAudit(string? actorUserId, string entityType, string entityId, string action, string summary, string? reason)
    {
        _dbContext.AdminAuditLogs.Add(new AdminAuditLogEntity
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Summary = summary,
            Reason = reason,
            CreatedUtc = DateTimeOffset.UtcNow
        });
    }

    public static string HashPin(string pin)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"LoveBrackley:{pin}"));
        return Convert.ToHexString(bytes);
    }

    private static void ValidatePin(string pin)
    {
        if (pin.Length < 4 || !pin.All(char.IsDigit))
        {
            ThrowValidation("pin", "PIN must contain at least 4 digits.");
        }
    }

    private static string CreateReference(string prefix) => $"LB-{prefix}-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{RandomNumberGenerator.GetInt32(1000, 9999)}";

    private static decimal PointsToPounds(int points, int pencePerPoint = 1) => decimal.Round(points * pencePerPoint / 100m, 2);

    private static int NormalizePage(int page) => page < 1 ? 1 : page;

    private static int NormalizePageSize(int pageSize) => pageSize is < 1 or > 100 ? 20 : pageSize;

    private static string? TrimToNull(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ThrowValidation(string propertyName, string message)
    {
        throw new ValidationException([new ValidationFailure(propertyName, message)]);
    }
}
