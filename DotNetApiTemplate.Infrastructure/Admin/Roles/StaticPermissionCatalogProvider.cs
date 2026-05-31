using DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;
using DotNetApiTemplate.Application.Features.Admin.Roles.Interfaces;

namespace DotNetApiTemplate.Infrastructure.Admin.Roles;

public sealed class StaticPermissionCatalogProvider : IPermissionCatalogProvider
{
    private static readonly AdminPermissionModel[] Permissions =
    [
        new() { Code = "users.view", Description = "View users." },
        new() { Code = "users.create", Description = "Create users." },
        new() { Code = "users.update", Description = "Update users." },
        new() { Code = "users.delete", Description = "Delete users." },
        new() { Code = "users.activate", Description = "Activate users." },
        new() { Code = "users.deactivate", Description = "Deactivate users." },
        new() { Code = "roles.view", Description = "View roles." },
        new() { Code = "roles.create", Description = "Create roles." },
        new() { Code = "roles.update", Description = "Update roles." },
        new() { Code = "roles.delete", Description = "Delete roles." },
        new() { Code = "roles.assign", Description = "Assign roles." },
        new() { Code = "permissions.view", Description = "View permissions." },
        new() { Code = "permissions.assign", Description = "Assign permissions." },
        new() { Code = "admin.view", Description = "Access admin area." },
        new() { Code = "admin.configure", Description = "Configure admin settings." },
        new() { Code = "audit.view", Description = "View audit entries." }
    ];

    public AdminPermissionModel[] GetPermissions()
    {
        return Permissions
            .Select(permission => new AdminPermissionModel
            {
                Code = permission.Code,
                Description = permission.Description
            })
            .ToArray();
    }
}
