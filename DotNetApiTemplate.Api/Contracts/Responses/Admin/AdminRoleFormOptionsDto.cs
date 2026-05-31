namespace DotNetApiTemplate.Api.Contracts.Responses.Admin;

public sealed class AdminRoleFormOptionsDto
{
    public AdminRoleParentOptionDto[] AvailableParentRoles { get; init; } = [];
    public AdminPermissionDto[] Permissions { get; init; } = [];
    public AdminPermissionDto[] AvailablePermissions { get; init; } = [];
    public string? ProtectedRootRoleId { get; init; }
}
