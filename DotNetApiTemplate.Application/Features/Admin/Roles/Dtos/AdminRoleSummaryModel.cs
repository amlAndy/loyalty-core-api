namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public class AdminRoleSummaryModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentRoleId { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsRootRole { get; init; }
    public string[] Permissions { get; init; } = [];
    public string CreatedAt { get; init; } = string.Empty;
    public string UpdatedAt { get; init; } = string.Empty;
    public string? ParentRoleName { get; init; }
    public int ChildCount { get; init; }
    public bool IsProtected { get; init; }
}
