namespace DotNetApiTemplate.Application.Features.Admin.Roles.Dtos;

public sealed class AdminRoleDefinitionModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string? ParentRoleId { get; init; }
    public bool IsSystemRole { get; init; }
    public bool IsRootRole { get; init; }
    public string[] Permissions { get; init; } = [];
    public DateTimeOffset CreatedAtUtc { get; init; }
    public DateTimeOffset UpdatedAtUtc { get; init; }
}
