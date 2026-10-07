using System.Security.Claims;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class SelectedRoleAuthorizationHandler : AuthorizationHandler<IAuthorizationRequirement>
{
    private const string PermissionClaimType = "permission";
    private readonly ApplicationDbContext _dbContext;

    public SelectedRoleAuthorizationHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        IAuthorizationRequirement requirement)
    {
        switch (requirement)
        {
            case ActiveRoleRequirement activeRoleRequirement:
                await HandleActiveRoleRequirementAsync(context, activeRoleRequirement);
                break;
            case ActiveRolePermissionRequirement permissionRequirement:
                await HandleActiveRolePermissionRequirementAsync(context, permissionRequirement);
                break;
        }
    }

    private async Task HandleActiveRoleRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveRoleRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        var activeRoleId = context.User.FindFirstValue(JwtTokenService.ActiveRoleIdClaimType);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(activeRoleId))
        {
            return;
        }

        var roleName = await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.RoleId == activeRoleId)
            .Join(
                _dbContext.Roles,
                userRole => userRole.RoleId,
                role => role.Id,
                (_, role) => role.Name)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(roleName))
        {
            return;
        }

        if (requirement.AllowedRoles.Any(allowedRole => string.Equals(allowedRole, roleName, StringComparison.OrdinalIgnoreCase)))
        {
            context.Succeed(requirement);
        }
    }

    private async Task HandleActiveRolePermissionRequirementAsync(
        AuthorizationHandlerContext context,
        ActiveRolePermissionRequirement requirement)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User.FindFirstValue("sub");
        var activeRoleId = context.User.FindFirstValue(JwtTokenService.ActiveRoleIdClaimType);

        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(activeRoleId))
        {
            return;
        }

        var hasPermission = await _dbContext.UserRoles
            .Where(userRole => userRole.UserId == userId && userRole.RoleId == activeRoleId)
            .Join(
                _dbContext.RoleClaims.AsNoTracking(),
                userRole => userRole.RoleId,
                roleClaim => roleClaim.RoleId,
                (_, roleClaim) => roleClaim)
            .AnyAsync(roleClaim =>
                roleClaim.ClaimType == PermissionClaimType &&
                roleClaim.ClaimValue == requirement.PermissionCode);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
