namespace DotNetApiTemplate.Application.Features.Admin.Authorization;

public static class AdminPermissionPolicies
{
    public const string UsersView = "users.view";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";
    public const string UsersActivate = "users.activate";
    public const string UsersDeactivate = "users.deactivate";

    public const string RolesView = "roles.view";
    public const string RolesCreate = "roles.create";
    public const string RolesUpdate = "roles.update";
    public const string RolesDelete = "roles.delete";
    public const string RolesAssign = "roles.assign";

    public const string PermissionsView = "permissions.view";
    public const string PermissionsAssign = "permissions.assign";

    public const string AdminView = "admin.view";
    public const string AdminConfigure = "admin.configure";
    public const string AuditView = "audit.view";
}
