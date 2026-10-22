using DotNetApiTemplate.Application.Features.Auth.Dtos;
using DotNetApiTemplate.Application.Features.LoveBrackley.Authorization;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.LoveBrackley;

public sealed class BusinessAccessAuthorizationService : IBusinessAccessAuthorizationService
{
    private const string ActiveStatus = "Active";
    private const string ActiveStatusUpper = "ACTIVE";

    private readonly ApplicationDbContext _dbContext;

    public BusinessAccessAuthorizationService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BusinessAccessAuthorizationResult> AuthorizeAsync(
        string? userId,
        string? activeRoleId,
        Guid businessId,
        BusinessAccessOperation operation,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(activeRoleId))
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        var business = await _dbContext.BusinessProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == businessId, cancellationToken);

        if (business is null)
        {
            return BusinessAccessAuthorizationResult.NotFound;
        }

        var activeRoleName = await ResolveRoleNameAsync(userId, activeRoleId, cancellationToken);
        if (string.IsNullOrWhiteSpace(activeRoleName))
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        var isUserActive = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(candidate =>
                candidate.Id == userId
                && candidate.Status != null
                && candidate.Status.ToUpper() == ActiveStatusUpper, cancellationToken);

        if (!isUserActive)
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        var isTransactional = operation is BusinessAccessOperation.EarnPoints or BusinessAccessOperation.RedeemPoints;

        if (string.Equals(activeRoleName, ApplicationRoleNames.BusinessOwner, StringComparison.OrdinalIgnoreCase))
        {
            return business.OwnerUserId == userId && (!isTransactional || IsBusinessActive(business.ParticipationStatus))
                ? BusinessAccessAuthorizationResult.Allowed
                : BusinessAccessAuthorizationResult.Forbidden;
        }

        if (!string.Equals(activeRoleName, ApplicationRoleNames.BusinessStaff, StringComparison.OrdinalIgnoreCase))
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        var matchingStaffAccesses = await _dbContext.BusinessStaffAccesses
            .AsNoTracking()
            .Where(candidate => candidate.BusinessProfileId == businessId && candidate.UserId == userId)
            .OrderBy(candidate => candidate.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matchingStaffAccesses.Count != 1)
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        var staff = matchingStaffAccesses[0];
        if (!IsActiveStaffStatus(staff.AccessStatus))
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        if (IsOwnerOnlyOperation(operation))
        {
            return BusinessAccessAuthorizationResult.Forbidden;
        }

        return !isTransactional || IsBusinessActive(business.ParticipationStatus)
            ? BusinessAccessAuthorizationResult.Allowed
            : BusinessAccessAuthorizationResult.Forbidden;
    }

    private async Task<string?> ResolveRoleNameAsync(string userId, string activeRoleId, CancellationToken cancellationToken)
    {
        return await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.RoleId == activeRoleId)
            .Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static bool IsOwnerOnlyOperation(BusinessAccessOperation operation)
    {
        return operation is BusinessAccessOperation.StaffAccessOverview
            or BusinessAccessOperation.StaffAccessSearch
            or BusinessAccessOperation.StaffAccessCreate
            or BusinessAccessOperation.StaffPinReset
            or BusinessAccessOperation.PayoutOverview
            or BusinessAccessOperation.PayoutSearch
            or BusinessAccessOperation.SettingsRead
            or BusinessAccessOperation.SettingsUpdate;
    }

    private static bool IsBusinessActive(string participationStatus)
    {
        return string.Equals(participationStatus, "Active", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsActiveStaffStatus(string pinStatus)
    {
        return string.Equals(pinStatus, "Active", StringComparison.OrdinalIgnoreCase);
    }
}
