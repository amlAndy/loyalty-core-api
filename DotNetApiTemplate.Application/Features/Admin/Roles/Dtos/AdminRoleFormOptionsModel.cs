namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleFormOptionsModel
{
    public AdminRoleParentOptionModel[] AvailableParentRoles { get; init; } = [];
    public AdminPermissionModel[] Permissions { get; init; } = [];
    public AdminPermissionModel[] AvailablePermissions { get; init; } = [];
    public string? ProtectedRootRoleId { get; init; }
}
