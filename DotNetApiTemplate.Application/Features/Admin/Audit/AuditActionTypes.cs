namespace DotNetApiTemplate.Application.Features.Admin.Audit;

public static class AuditActionTypes
{
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";
    public const string UserDeleted = "UserDeleted";
    public const string UserStatusChanged = "UserStatusChanged";
    public const string UserRoleAssigned = "UserRoleAssigned";
    public const string UserRoleUnassigned = "UserRoleUnassigned";
    public const string RoleCreated = "RoleCreated";
    public const string RoleUpdated = "RoleUpdated";
    public const string RoleDeleted = "RoleDeleted";
    public const string RolePermissionAssigned = "RolePermissionAssigned";
    public const string RolePermissionUnassigned = "RolePermissionUnassigned";

    public static readonly string[] Values =
    [
        UserCreated,
        UserUpdated,
        UserDeleted,
        UserStatusChanged,
        UserRoleAssigned,
        UserRoleUnassigned,
        RoleCreated,
        RoleUpdated,
        RoleDeleted,
        RolePermissionAssigned,
        RolePermissionUnassigned
    ];
}
