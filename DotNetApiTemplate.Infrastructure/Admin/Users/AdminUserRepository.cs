using DotNetApiTemplate.Application.Features.Admin.Users;
using DotNetApiTemplate.Application.Features.Admin.Users.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Users.Interfaces;
using DotNetApiTemplate.Infrastructure.Identity;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Admin.Users;

public sealed class AdminUserRepository : IAdminUserRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminUserRepository(
        ApplicationDbContext dbContext,
        UserManager<ApplicationUser> userManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
    }

    public async Task<AdminUserDetailModel> CreateAsync(
        AdminUserUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            DisplayName = model.DisplayName,
            Status = model.Status
        };

        await ThrowIfIdentityFailedAsync(_userManager.CreateAsync(user));
        await ReplaceUserRolesAsync(user.Id, model.RoleIds, cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken)
            ?? throw new InvalidOperationException("Created user could not be reloaded.");
    }

    public async Task<bool> DeleteAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        if (user is null)
        {
            return false;
        }

        await ThrowIfIdentityFailedAsync(_userManager.DeleteAsync(user));

        return true;
    }

    public Task<bool> EmailExistsAsync(
        string email,
        string? excludeUserId = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.ToUpper();

        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(
                user => user.NormalizedEmail == normalizedEmail && (excludeUserId == null || user.Id != excludeUserId),
                cancellationToken);
    }

    public async Task<string[]> GetExistingRoleIdsAsync(
        string[] roleIds,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Roles
            .AsNoTracking()
            .Where(role => roleIds.Contains(role.Id))
            .Select(role => role.Id)
            .Distinct()
            .ToArrayAsync(cancellationToken);
    }

    public async Task<AdminUserFormOptionsModel> GetFormOptionsAsync(
        CancellationToken cancellationToken = default)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .ThenBy(role => role.Id)
            .Select(role => new AdminRoleOptionModel
            {
                Id = role.Id,
                Name = role.Name ?? string.Empty,
                ParentRoleId = role.ParentRoleId,
                IsRootRole = role.IsRootRole,
                IsSystemRole = role.IsSystemRole
            })
            .ToArrayAsync(cancellationToken);

        return new AdminUserFormOptionsModel
        {
            Statuses = AdminUserStatuses.Values,
            Roles = roles
        };
    }

    public async Task<AdminUserDetailModel?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == id)
            .Select(user => new UserProjection(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.Status))
            .SingleOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roleData = await GetRoleDataAsync([user.Id], cancellationToken);
        var roles = roleData.TryGetValue(user.Id, out var value)
            ? value
            : EmptyRoleData;

        return new AdminUserDetailModel
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Status = user.Status,
            RoleIds = roles.RoleIds,
            Roles = roles.RoleNames
        };
    }

    public async Task<AdminUserSearchResult> SearchUsersAsync(
        AdminUserSearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        var usersQuery = _dbContext.Users
            .AsNoTracking()
            .AsQueryable();
        var pageNumber = criteria.PageNumber ?? 1;
        var pageSize = criteria.PageSize ?? 10;

        if (!string.IsNullOrWhiteSpace(criteria.Query))
        {
            var normalizedQuery = criteria.Query.Trim().ToUpper();
            usersQuery = usersQuery.Where(user =>
                user.DisplayName.ToUpper().Contains(normalizedQuery) ||
                (user.Email ?? string.Empty).ToUpper().Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(criteria.RoleId))
        {
            var roleId = criteria.RoleId.Trim();
            usersQuery = usersQuery.Where(user =>
                _dbContext.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == roleId));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            var normalizedStatus = criteria.Status.Trim().ToUpper();
            usersQuery = usersQuery.Where(user => user.Status.ToUpper() == normalizedStatus);
        }

        var total = await usersQuery.CountAsync(cancellationToken);

        if (total == 0)
        {
            return new AdminUserSearchResult
            {
                Items = [],
                Total = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }

        var users = await usersQuery
            .OrderBy(user => user.DisplayName)
            .ThenBy(user => user.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(user => new UserProjection(
                user.Id,
                user.DisplayName,
                user.Email ?? string.Empty,
                user.Status))
            .ToArrayAsync(cancellationToken);

        var userIds = users.Select(user => user.Id).ToArray();
        var rolesByUserId = await GetRoleDataAsync(userIds, cancellationToken);

        return new AdminUserSearchResult
        {
            Items = users.Select(user =>
            {
                var roleData = rolesByUserId.TryGetValue(user.Id, out var value)
                    ? value
                    : EmptyRoleData;

                return new AdminUserSummaryModel
                {
                    Id = user.Id,
                    DisplayName = user.DisplayName,
                    Email = user.Email,
                    Status = user.Status,
                    RoleIds = roleData.RoleIds,
                    Roles = roleData.RoleNames
                };
            }).ToArray(),
            Total = total,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<AdminUserDetailModel?> UpdateAsync(
        string id,
        AdminUserUpsertModel model,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

        if (user is null)
        {
            return null;
        }

        user.DisplayName = model.DisplayName;
        user.Email = model.Email;
        user.UserName = model.Email;
        user.Status = model.Status;

        await ThrowIfIdentityFailedAsync(_userManager.UpdateAsync(user));
        await ReplaceUserRolesAsync(user.Id, model.RoleIds, cancellationToken);

        return await GetByIdAsync(user.Id, cancellationToken);
    }

    private sealed record UserProjection(string Id, string DisplayName, string Email, string Status);

    private sealed record UserRoleProjection(string UserId, string RoleId, string RoleName);

    private sealed record RoleData(string[] RoleIds, string[] RoleNames);

    private static readonly RoleData EmptyRoleData = new([], []);

    private async Task ReplaceUserRolesAsync(
        string userId,
        string[] roleIds,
        CancellationToken cancellationToken)
    {
        var existingUserRoles = await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId)
            .ToArrayAsync(cancellationToken);

        if (existingUserRoles.Length > 0)
        {
            _dbContext.UserRoles.RemoveRange(existingUserRoles);
        }

        var distinctRoleIds = roleIds.Distinct(StringComparer.Ordinal).ToArray();

        if (distinctRoleIds.Length > 0)
        {
            _dbContext.UserRoles.AddRange(distinctRoleIds.Select(roleId => new IdentityUserRole<string>
            {
                UserId = userId,
                RoleId = roleId
            }));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task ThrowIfIdentityFailedAsync(Task<IdentityResult> identityTask)
    {
        var result = await identityTask;

        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join("; ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException(errors);
    }

    private async Task<Dictionary<string, RoleData>> GetRoleDataAsync(
        string[] userIds,
        CancellationToken cancellationToken)
    {
        var userRoles = await (
            from userRole in _dbContext.UserRoles.AsNoTracking()
            join role in _dbContext.Roles.AsNoTracking() on userRole.RoleId equals role.Id
            where userIds.Contains(userRole.UserId)
            select new UserRoleProjection(userRole.UserId, role.Id, role.Name ?? string.Empty))
            .ToArrayAsync(cancellationToken);

        return userRoles
            .GroupBy(userRole => userRole.UserId)
            .ToDictionary(
                group => group.Key,
                group => new RoleData(
                    group.Select(item => item.RoleId).Distinct().ToArray(),
                    group.Select(item => item.RoleName).Distinct().ToArray()));
    }
}
