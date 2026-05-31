namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleUpsertModel
{
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentRoleId { get; init; }
    public string[] Permissions { get; init; } = [];
}
