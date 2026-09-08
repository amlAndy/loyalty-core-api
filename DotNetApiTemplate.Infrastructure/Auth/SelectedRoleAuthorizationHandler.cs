using System.Security.Claims;
using DotNetApiTemplate.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class SelectedRoleAuthorizationHandler : AuthorizationHandler<ActiveRoleRequirement>
{
    private readonly ApplicationDbContext _dbContext;

    public SelectedRoleAuthorizationHandler(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
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
}
