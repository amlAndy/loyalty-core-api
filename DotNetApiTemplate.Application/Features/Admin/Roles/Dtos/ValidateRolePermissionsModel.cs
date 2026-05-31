namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class ValidateRolePermissionsModel
{
    public string? ParentRoleId { get; init; }
    public string[] Permissions { get; init; } = [];
    public string? CurrentRoleId { get; init; }
}
