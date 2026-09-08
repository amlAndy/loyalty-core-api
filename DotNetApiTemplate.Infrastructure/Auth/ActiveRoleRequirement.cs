using Microsoft.AspNetCore.Authorization;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class ActiveRoleRequirement : IAuthorizationRequirement
{
    public ActiveRoleRequirement(params string[] allowedRoles)
    {
        AllowedRoles = allowedRoles.Length == 0 ? [] : allowedRoles;
    }

    public IReadOnlyCollection<string> AllowedRoles { get; }
}
