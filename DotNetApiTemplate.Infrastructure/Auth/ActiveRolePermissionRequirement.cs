using Microsoft.AspNetCore.Authorization;

namespace DotNetApiTemplate.Infrastructure.Auth;

public sealed class ActiveRolePermissionRequirement : IAuthorizationRequirement
{
    public ActiveRolePermissionRequirement(string permissionCode)
    {
        PermissionCode = string.IsNullOrWhiteSpace(permissionCode)
            ? throw new ArgumentException("Permission code is required.", nameof(permissionCode))
            : permissionCode;
    }

    public string PermissionCode { get; }
}
